using System;
using osum.Helpers;
using osum.Libraries.NetLib;
using osum.UI;

namespace osum {
    public partial class GameBase /* Arcade Authentication */ {
        /// <summary>
        /// Is the card reading loading screen active?
        /// This is used because the pico pretty much spams CardSignal
        /// requests, so we don't open 50 billion loading screen
        /// </summary>
        private bool _cardLoadingSpinnerActive = false;
        private bool _loginProcessOccuring = false;

        /// <summary>
        /// Notificaion with the loading screen showing
        /// </summary>
        private Notification _cardLoadingNotification;
        private Notification _newOrLinkToExistingNotification;
        private Notification _pinEntryNotification;
        private Notification _cardReadErrNotification;

        /// <summary>
        /// Starts the Arcade Login process
        /// </summary>
        /// <param name="cardId">Raw Card ID as received from the Pico 2</param>
        /// <param name="cardType">Card Type as received from the Pico 2</param>
        /// <param name="cardName">Card Name as received from the Pico 2</param>
        public void ArcadeStartLoginProcess(string cardId, string cardType, string cardName) {
            _loginProcessOccuring = true;

            //Censoring the Card ID, Konami arcades do this too
            string censoredCardId = cardId.Substring(0, 3);

            for (int i = 0; i != (cardId.Length - 6); i++) {
                censoredCardId += 'x';
            }

            censoredCardId += cardId.Substring(cardId.Length - 6, 3);

            Console.WriteLine("got to censoring card id");

            Scheduler.Add(() => {
                if (_cardLoadingNotification == null) {
                    _cardLoadingNotification = new Notification("Logging in...", "", NotificationStyle.Loading);
                }

                if (cardName != "") {
                    this._cardLoadingNotification.descriptionText.Text = "Attempting login using " + cardName + " Card with ID:\n" + censoredCardId;
                } else {
                    this._cardLoadingNotification.descriptionText.Text = "Attempting login using " + cardType + " Card with ID:\n" + censoredCardId;
                }

                Console.WriteLine("notif description set");

                string machineKey = Config.GetValue("MachineKey", "");

                StringNetRequest arcadeAuth = new StringNetRequest($"http://localhost:80/stream/arcade-auth?cardId={cardId}&machineKey={machineKey}");

                arcadeAuth.onFinish += (result, exception) => {
                    if (result == "") {
                        loginFailed();
                        return;
                    }

                    switch (result) {
                        case "new":
                            this.RegisterNewCard(cardId, cardType, cardName);
                            return;
                        case "cardDisabled":
                            this.ReportCardDisabled();
                            return;
                        case "noMachine":
                            this.ReportMachineNotRegistered();
                            return;
                        case "machineDisabled":
                            this.ReportMachineDisabled();
                            return;
                        case "exists":
                            this.LoginExistingCard(cardId);
                            break;
                        default:
                            if (exception != null) {
                                this.loginFailed();
                            }

                            break;
                    }
                };

                NetManager.AddRequest(arcadeAuth);
            }, 1000);
        }

        private void loginFailed() {
            this._cardLoadingNotification.Dismiss(false);

            Notify(new Notification(
               "Error has occured!",
               "Login failed! Please try again later, or continue as guest!",
               NotificationStyle.Okay
            ));

            this._cardLoadingSpinnerActive = false;
            this._loginProcessOccuring     = false;
        }

        #region Existing Card Login

        private void doCardLoginWithValues(string cardId, string cardPin, Action<bool> onComplete = null) {
            string machineKey = Config.GetValue("MachineKey", "");

            StringNetRequest tokenRequest = new StringNetRequest($"http://localhost:80/stream/arcade-auth?cardId={cardId}&cardPin={cardPin}&machineKey={machineKey}");

            StringNetRequest.RequestCompleteHandler tokenRequestFinish = null; tokenRequestFinish = (result, exception) => {
                tokenRequest.onFinish -= tokenRequestFinish;

                switch (result) {
                    case "":
                        this.loginFailed();
                        onComplete?.Invoke(false);
                        return;
                    case "noMachine":
                        this.ReportMachineNotRegistered();
                        onComplete?.Invoke(false);
                        return;
                    case "machineDisabled":
                        this.ReportMachineDisabled();
                        onComplete?.Invoke(false);
                        return;
                    case "cardDisabled":
                        this.ReportCardDisabled();
                        onComplete?.Invoke(false);
                        return;
                    case "pin":
                        Notify(new Notification(
                            "Authentication failed!",
                            "The PIN code you entered is invalid!",
                            NotificationStyle.Okay
                        ));
                        onComplete?.Invoke(false);
                        return;
                    case "exists":
                    case "new":
                        onComplete?.Invoke(false);
                        this.loginFailed();
                        return;
                    default:
                        string[] splitData = result.Split('\n');

                        if (splitData.Length != 3) {
                            this.loginFailed();
                            return;
                        }

                        onComplete?.Invoke(true);

                        EstablishUser(splitData[1], splitData[2]);

                        break;
                }

            };

            tokenRequest.onFinish += tokenRequestFinish;

            NetManager.AddRequest(tokenRequest);
        }

        private void LoginExistingCard(string cardId) {
            _cardLoadingNotification.Dismiss(true);

            _pinEntryNotification = new Notification(
                "Welcome to osu!arcade!",
                "Please enter your Card PIN for authentication using the keypad on the side.",
                NotificationStyle.PinEntry
            );

            //Stacked cuz I don't wanna waste screen space because of a dumb compiler error
            Notification.InputEntryCompletedDelegate inputEntryCompleteHandler = null; inputEntryCompleteHandler = notification => {
                _pinEntryNotification.InputEntryComplete -= inputEntryCompleteHandler;

                _pinEntryNotification.Dismiss(true);

                doCardLoginWithValues(cardId, notification.EnteredInput);
            };

            _pinEntryNotification.InputEntryComplete += inputEntryCompleteHandler;

            Notify(_pinEntryNotification);
        }

        #endregion

        #region New Card Registration

        private void RegisterNewCard(string cardId, string cardType, string cardName) {
            this._cardLoadingNotification.Dismiss(true);

            this._newOrLinkToExistingNotification = new Notification(
                "Welcome to osu!arcade!",
                "Your card is new on the server, do you intend to create a new Account or link this card to an existing account?",
                NotificationStyle.YesNo
            );

            this._newOrLinkToExistingNotification.yesText.Text = "New";
            this._newOrLinkToExistingNotification.noText.Text = "Link";

            BoolDelegate newOrLinkEventHandler = @new => {
                if (@new) {
                    this._newOrLinkToExistingNotification.Action = null;
                    this.InvokeAccountCreationScreen(cardId, cardType, cardName);
                } else {
                    Notification pinRevealNotif = new Notification(
                        "Card Linking",
                        "Below is your Card ID required for linking another card to your account.",
                        NotificationStyle.HiddenText,
                        b => this._cardLoadingSpinnerActive = false
                    );

                    pinRevealNotif.TextToHide = cardId;

                    Notify(pinRevealNotif);

                    this._newOrLinkToExistingNotification.Action = null;

                    this._loginProcessOccuring     = false;
                    this._cardLoadingSpinnerActive = false;
                }
            };

            this._newOrLinkToExistingNotification.Action                   = newOrLinkEventHandler;
            this._newOrLinkToExistingNotification.descriptionText.TextSize = 24;

            Notify(this._newOrLinkToExistingNotification);
        }

        #endregion

        private bool _usernameCheckRunning   = false;
        private bool _usernameEntryHappening = false;

        private void InvokeAccountCreationScreen(string cardId, string cardType, string cardName) {
            if (this._usernameEntryHappening) {
                return;
            }

            this._usernameEntryHappening = true;

            Notification usernameInputNotification = new Notification(
                "Account Creation",
                "Please enter your Username for your new Account. This username has to be unique on the server",
                NotificationStyle.TextInput
            );

            usernameInputNotification.descriptionText.TextSize = 18;

            Notification.InputEntryCompletedDelegate usernameEntryComplete = null; usernameEntryComplete = sender => {
                if (this._usernameCheckRunning) {
                    return;
                }

                this._usernameCheckRunning                     = true;
                usernameInputNotification.descriptionText.Text = "Checking username...";

                StringNetRequest usernameDuplicateCheck = new StringNetRequest($"http://localhost:80/stream/arcade-username?username={sender.EnteredInput}");

                StringNetRequest.RequestCompleteHandler usernameDuplicateCheckComplete = null; usernameDuplicateCheckComplete = (result, exception) => {
                    usernameDuplicateCheck.onFinish -= usernameDuplicateCheckComplete;

                    if (exception != null || string.IsNullOrEmpty(result)) {
                        usernameInputNotification.descriptionText.Text = "Username check failed! Please try again.";

                        this._usernameCheckRunning                   =  false;
                        this._usernameEntryHappening                 =  false;
                        usernameInputNotification.InputEntryComplete -= usernameEntryComplete;

                        usernameInputNotification.Dismiss(true);

                        loginFailed();
                        return;
                    }

                    if (result == "taken") {
                        usernameInputNotification.descriptionText.Text = "Username has been taken! Please try a different username.";

                        this._usernameCheckRunning   = false;
                        return;
                    }

                    usernameInputNotification.Dismiss(true);

                    this._usernameCheckRunning                   =  false;
                    this._usernameEntryHappening                 =  false;
                    usernameInputNotification.InputEntryComplete -= usernameEntryComplete;

                    AccountCreationPinEntry(cardId, cardType, cardName, sender.EnteredInput);
                };

                usernameDuplicateCheck.onFinish += usernameDuplicateCheckComplete;

                NetManager.AddRequest(usernameDuplicateCheck);
            };

            usernameInputNotification.InputEntryComplete += usernameEntryComplete;

            Notify(usernameInputNotification);
        }

        private void AccountCreationPinEntry(string cardId, string cardType, string cardName, string username) {
            Notification pinInputNotification = new Notification(
                "Account Creation",
                "Additionally, please enter a PIN for your accounts security. This PIN will be required every time your card is swiped.",
                NotificationStyle.PinEntry
            );

            pinInputNotification.descriptionText.TextSize = 18;

            Notification.InputEntryCompletedDelegate pinEntryComplete = null; pinEntryComplete = sender => {
                pinInputNotification.InputEntryComplete -= pinEntryComplete;

                string machineKey = Config.GetValue("MachineKey", "");

                pinInputNotification.Dismiss(true);

                Notification accountCreationHappening = new Notification(
                    "Account Creation",
                    "Attempting registration now...",
                    NotificationStyle.Loading
                );

                Notify(accountCreationHappening);

                StringNetRequest accountCreationRequest = new StringNetRequest($"http://localhost:80/stream/arcade-register?username={username}&pin={sender.EnteredInput}&cardId={cardId}&cardType={cardType}&cardName={cardName}&machineKey={machineKey}");

                StringNetRequest.RequestCompleteHandler accountCreationRequestComplete = null; accountCreationRequestComplete = (result, exception) => {
                    accountCreationRequest.onFinish -= accountCreationRequestComplete;

                    Action accountCreationFailed = delegate {
                        Scheduler.Add(delegate {
                            Notify(new Notification(
                                "Account Creation",
                                "Account creation failed! Try again later!",
                                NotificationStyle.Okay
                            ));
                        }, 1000);

                        this._cardLoadingSpinnerActive = false;
                        this._loginProcessOccuring     = false;
                    };

                    if (exception != null && string.IsNullOrEmpty(result)) {
                        accountCreationHappening.Dismiss(true);
                        accountCreationFailed();

                        return;
                    }

                    switch (result) {
                        case "":
                            accountCreationHappening.Dismiss(true);
                            accountCreationFailed();
                            return;
                        case "noMachine":
                            accountCreationHappening.Dismiss(true);
                            this.ReportMachineNotRegistered();
                            accountCreationFailed();
                            return;
                        case "machineDisabled":
                            accountCreationHappening.Dismiss(true);
                            this.ReportMachineDisabled();
                            accountCreationFailed();
                            return;
                        case "username":
                        case "card":
                            accountCreationHappening.Dismiss(true);

                            Notify(new Notification(
                                "Account Creation",
                                "Somehow either your card or username is already registered? This is a strange occurance, contact the machine owner/server host",
                                NotificationStyle.Okay
                            ));

                            accountCreationFailed();
                            return;
                        case "ok":
                            doCardLoginWithValues(cardId, sender.EnteredInput,  delegate(bool loginSuccess) {
                                accountCreationHappening.Dismiss(true);
                            });
                            return;
                    }
                };

                accountCreationRequest.onFinish += accountCreationRequestComplete;

                NetManager.AddRequest(accountCreationRequest);
            };

            pinInputNotification.InputEntryComplete += pinEntryComplete;

            Notify(pinInputNotification);
        }

        #region Error Messages

        /// <summary>
        /// Reports the Machine being disabled, reasoning for implementing this at all can be read in the notification
        /// </summary>
        private void ReportMachineDisabled() {
            Notify(new Notification(
               "Machine disabled!",
               "This machine has been disabled! This usually is caused by the machine key being leaked. Or the machine owner installing modifications that provide unfair advantages to players using it!",
               NotificationStyle.Okay
            ));

            _loginProcessOccuring = false;
        }

        /// <summary>
        /// Reports the card being disabled, in the event of it being stolen or smth
        /// </summary>
        private void ReportCardDisabled() {
            Notify(new Notification(
               "Card disabled!",
               "This card has been disabled by the user. This means either you're using the wrong card or the card you're trying to use isn't yours!",
               NotificationStyle.Okay
            ));

            _loginProcessOccuring = false;
        }

        /// <summary>
        /// For security the server I (furball) made only allows certain arcades to register.
        /// As long as the arcade doesnt allow crazy shenanigans that nobody else has
        /// theres a near 100% chance that ill allow them on the network.
        /// self hosting of course is a thing too, so if you dont like me or smth feel free
        /// to host your arcades on your own networks. my server will also allow syncing of data between servers
        /// to make it as seamless as possible for players
        /// </summary>
        private void ReportMachineNotRegistered() {
            Notify(new Notification(
               "Unregistered machine!",
               "This machine has not yet been registered on the osu!arcade server that has been configured. Contact the machine owner!",
               NotificationStyle.Okay
            ));

            _loginProcessOccuring = false;
        }

        #endregion
    }
}
