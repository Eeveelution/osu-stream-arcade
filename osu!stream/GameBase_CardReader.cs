using System;
using osum.Input;
using osum.UI;

namespace osum {
    public partial class GameBase {
        private string _lastReceivedCardId, _lastReceivedCardType;

        private void displayCardLoadingSpinner(bool createHidIoTimeout = false) {
            if (!this._cardLoadingSpinnerActive) {
                _cardLoadingSpinnerActive = true;

                _cardLoadingNotification = new Notification("Logging into the osu!arcade network!", "Attempting to retrieve information about card.", NotificationStyle.Loading);

                Notify(this._cardLoadingNotification);

                if (createHidIoTimeout) {
                    Scheduler.Add(() => {
                        if (!this._loginProcessOccuring) {
                            _cardLoadingSpinnerActive = true;

                            _cardLoadingNotification = new Notification("Logging into the osu!arcade network!", "", NotificationStyle.Loading);

                            Notify(this._cardLoadingNotification);

                            Console.WriteLine("nosz kurwa: lastReceivedCardId: " + this._lastReceivedCardId);

                            this.ArcadeStartLoginProcess(this._lastReceivedCardId, this._lastReceivedCardType, "");
                        } else Console.WriteLine("nosz kurwa: login process occuring. aborting");
                    }, 1000);
                }
            }
        }

        public void CardReaderThread() {
            try {
                while (this._cardReaderPort.IsOpen) {
                    string newLine = this._cardReaderPort.ReadLine();

                    Console.WriteLine("NewAICIO: " + newLine);

                    string[] splitCommand = newLine.Split('|');

                    if (splitCommand.Length >= 1) {
                        switch (splitCommand[0]) {
                            case "CardSignal":
                                this.displayCardLoadingSpinner();
                                break;
                            case "CardData":
                                if (splitCommand.Length >= 3) {
                                    string cardType = splitCommand[1];
                                    string cardName = splitCommand[2];
                                    string cardId = splitCommand[3];

                                    if (!_loginProcessOccuring) {
                                        this.ArcadeStartLoginProcess(cardId, cardType, cardName);
                                    }
                                } else {
                                    loginFailed();
                                }
                                break;
                            default:
                                //Try to fallback to regular CardIO HID
                                string[] splitCardIO = newLine.Replace("-", "").Split('>');

                                if (splitCardIO.Length == 2 && splitCardIO[1].Contains("CardIO")) {
                                    string cardId = splitCardIO[1].Replace("CardIO", "").Replace(" ", "");

                                    Console.WriteLine("nosz kurwa: CardReaderThread read id: " + cardId);

                                    string[] cardTypePart = splitCardIO[0].Split(':');

                                    if (cardTypePart.Length == 2) {
                                        string cardType = cardTypePart[0].Replace(" ", "");

                                        switch (cardType) {
                                            case "MIFARE":
                                            case "FeliCa":
                                            case "15693":
                                            case "None":
                                                this._lastReceivedCardId   = cardId;
                                                this._lastReceivedCardType = cardType;

                                                if (this._lastReceivedCardId == "") {
                                                    Console.WriteLine("FIRST RECEIVED CARDID: " + this._lastReceivedCardId);
                                                } else {
                                                    Console.WriteLine("OVERWRITING CARDID WITH NEW RECEIVED: " + this._lastReceivedCardId);
                                                }

                                                this.displayCardLoadingSpinner(true);
                                                break;
                                            default:
                                                Console.WriteLine($"WEIRD CARD TYPE: \"{cardType}\"");
                                                break;
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
            }
            catch {}
        }
    }
}
