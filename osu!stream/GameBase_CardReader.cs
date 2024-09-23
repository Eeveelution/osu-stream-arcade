using System;
using osum.UI;

namespace osum {
    public partial class GameBase {
        public void CardReaderThread() {
            try {
                while (this._cardReaderPort.IsOpen) {
                    string newLine = this._cardReaderPort.ReadLine();

                    Console.WriteLine("NewAICIO: " + newLine);

                    string[] splitCommand = newLine.Split('|');

                    if (splitCommand.Length >= 1) {
                        switch (splitCommand[0]) {
                            case "CardSignal":
                                if (!this._cardLoadingSpinnerActive) {
                                    _cardLoadingSpinnerActive = true;

                                    _cardLoadingNotification = new Notification("Logging into the osu!arcade network!", "Attempting to retrieve information about card.", NotificationStyle.Loading);

                                    Notify(this._cardLoadingNotification);
                                }

                                //case "CardSignal"
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
                                //case "CardData"
                                break;
                            default:
                                if (!this._cardLoadingSpinnerActive) {
                                    //Try to fallback to regular CardIO HID
                                    string[] splitCardIO = newLine.Replace("-", "").Split('>');

                                    if (splitCardIO.Length == 2 && splitCardIO[1].Contains("CardIO")) {
                                        string cardId = splitCardIO[1].Replace("CardIO", "").Replace(" ", "");

                                        string[] cardTypePart = splitCardIO[0].Split(':');

                                        if (cardTypePart.Length == 2) {
                                            string cardType = cardTypePart[0].Replace(" ", "");

                                            switch (cardType) {
                                                case "MIFARE":
                                                case "FeliCa":
                                                case "15693":
                                                case "None":
                                                    if (!this._loginProcessOccuring) {
                                                        _cardLoadingSpinnerActive = true;

                                                        _cardLoadingNotification = new Notification("Logging into the osu!arcade network!", "", NotificationStyle.Loading);

                                                        Notify(this._cardLoadingNotification);

                                                        this.ArcadeStartLoginProcess(cardId, cardType, "");
                                                    }
                                                    break;
                                                default:
                                                    Console.WriteLine($"WEIRD CARD TYPE: \"{cardType}\"");
                                                    break;
                                            }


                                        }
                                    }
                                }
                                //default:
                                break;
                        }
                    }
                }
            }
            catch {}
        }
    }
}
