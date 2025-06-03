using System;
using System.Diagnostics;
using osum.GameModes;
using osum.GameModes.MainMenu;
using osum.Helpers;
using osum.UI;

namespace osum {
    public enum CreditType {
        Time,
        SongCount,
        FreePlay
    }

    public class ArcadeUserData {
        private static int CreditLength = 10 * 60 * 1000;
        //private const int CreditLength = 15000;

        /// <summary>
        /// Is the User logged in? Also counts if the Guest is logged in
        /// </summary>
        public static bool      HasAuth       = false;
        /// <summary>
        /// Is the current user a Guest? as in, have they not logged in with a e-amusement card?
        /// </summary>
        public static bool      IsGuest       = true;
        /// <summary>
        /// Token received during authentication used for song submission
        /// </summary>
        public static string    SubmitToken   = "";
        /// <summary>
        /// Player name, "Guest" if guest/
        /// </summary>
        public static string    Username      = "";
        public static ulong     UserId        = 0;
        /// <summary>
        /// Fun statistic that might make it into the game, based off volforce.
        /// </summary>
        public static double    StatStreams   = 0.0;

        /// <summary>
        /// What kind of Credit Type is configured. Time based? x Songs? Just forever until someone else appears?
        /// </summary>
        public static CreditType CreditType    = CreditType.Time;
        /// <summary>
        /// Stopwatch used for the Time based Credit
        /// </summary>
        public static Stopwatch  CreditCounter = new Stopwatch();
        /// <summary>
        /// Configured value for the default number of songs the player can play when x Songs is configuired as the credit
        /// </summary>
        public static int        SongCreditCountSongs = 3;
        /// <summary>
        /// Flag whether the credit has been started with x songs
        /// </summary>
        public static bool StartedSongCountCredit = false;
        /// <summary>
        /// Flag whether the credit has been started with freeplay
        /// </summary>
        public static bool StartedFreeplayCredit = false;
        /// <summary>
        /// How many more songs can the user play on this credit.
        /// </summary>
        public static int        SongCountLeft          = 0;

        private static Notification _thanksForPlayingNotification;

        private static long   _lastRemainingSeconds = 0;
        private static string _lastRemainingText    = "";

        public static void Initialize() {
            string creditType = GameBase.Config.GetValue("CreditType", "Time");

            bool creditTypeParseSuccess = Enum.TryParse(creditType, true, out CreditType);

            if (!creditTypeParseSuccess) {
                Console.WriteLine("Could not parse Credit Type. Defaulting to Time based Credit.");

                CreditType = CreditType.Time;
            }

            switch (CreditType) {
                case CreditType.Time: {
                    string creditTimeSeconds = GameBase.Config.GetValue("CreditTimeSeconds", "600");

                    bool parsedCreditTimeSuccess = int.TryParse(creditTimeSeconds, out int creditLength);

                    if (!parsedCreditTimeSuccess) {
                        Console.WriteLine("Could not parse Credit Time Seconds. Defaulting to 10 minutes.");
                    } else {
                        CreditLength = creditLength * 1000;
                    }
                    break;
                }
                case CreditType.SongCount: {
                    string creditSongCount = GameBase.Config.GetValue("CreditSongCount", "3");

                    bool parsedCreditSongCountSuccess = int.TryParse(creditSongCount, out SongCreditCountSongs);

                    if (!parsedCreditSongCountSuccess) {
                        Console.WriteLine("Could not parse Credit Song Count. Defaulting to 3 songs.");
                    }
                    break;
                }
            }
        }

        public static string GetFormattedCurrentSongText() {
            if (SongCountLeft == 1) {
                return "Final Song!";
            }

            int num = (SongCreditCountSongs - SongCountLeft) + 1;

            if (num <= 0) return $"{num} Song!";

            switch(num % 100)
            {
                case 11:
                case 12:
                case 13:
                    return $"{num}th Song!";
            }

            switch(num % 10)
            {
                case 1:
                    return $"{num}st Song!";
                case 2:
                    return $"{num}nd Song!";
                case 3:
                    return $"{num}rd Song!";
                default:
                    return $"{num}th Song!";
            }
        }

        public static void DoSongCountNotification() {
            GameBase.Notify(new Notification(
                    "",
                    GetFormattedCurrentSongText(),
                    NotificationStyle.Brief
            ));
        }

        /// <summary>
        /// Formats the remaining time when the Time based credit is configured
        /// </summary>
        /// <returns></returns>
        public static string GetFormattedRemainingTime() {
            if (CreditType != CreditType.Time) {
                return "";
            }

            long remainingSeconds = (long)( (double)(CreditLength - CreditCounter.ElapsedMilliseconds) / 1000.0f);

            if (_lastRemainingSeconds == remainingSeconds) {
                return _lastRemainingText;
            }

            _lastRemainingSeconds = remainingSeconds;

            double minutes = Math.Max(Math.Floor((double) remainingSeconds / 60.0f), 0);

            remainingSeconds -= (long) (minutes * 60.0f);

            return _lastRemainingText = $"{(int) minutes}.{(int)Math.Max(remainingSeconds, 0):00}";
        }

        /// <summary>
        /// Universal method for starting a credit of any configurable type
        /// </summary>
        public static void StartCredit() {
            switch (CreditType) {
                case CreditType.Time:
                    CreditCounter.Start();
                    break;
                case CreditType.SongCount:
                    StartedSongCountCredit = true;
                    SongCountLeft = SongCreditCountSongs;
                    break;
                case CreditType.FreePlay:
                    StartedFreeplayCredit = true;
                    break;
            }


            if (IsGuest) {
                HasAuth  = true;
                Username = "Guest";
            }
        }

        public static bool CreditStarted() {
            return CreditCounter.ElapsedMilliseconds > 0 || StartedSongCountCredit || StartedFreeplayCredit;
        }

        public static bool CreditOver() {
            if (CreditType == CreditType.FreePlay) {
                return false;
            }

            return (CreditCounter.ElapsedMilliseconds >= CreditLength || (CreditType == CreditType.SongCount && SongCountLeft <= 0)) && CreditStarted();
        }

        public static void ResetLogin() {
            HasAuth                = false;
            IsGuest                = true;
            SubmitToken            = "";
            Username               = "";
            UserId                 = 0;
            StatStreams            = 0.0;
            CreditCounter          = new Stopwatch();
            StartedSongCountCredit = false;
            SongCountLeft          = SongCreditCountSongs;
            StartedFreeplayCredit  = false;
        }

        public static bool CreditOverReturnCatch(VoidDelegate preQuitTask = null) {
            if (CreditStarted() && CreditOver() && (_thanksForPlayingNotification == null || _thanksForPlayingNotification.Dismissed) ) {
                if (preQuitTask != null) {
                    preQuitTask();
                }

                string text = "";

                switch (CreditType) {
                    case CreditType.Time:
                        text = "Your timer has run out! Please look around and let others try out the arcade too!";
                        break;
                    case CreditType.SongCount:
                        text = "Final Song complete! Thanks for playing! Please look around and let others try out the arcade too!";
                        break;
                }

                if (!IsGuest) {
                    text += "\nPlease remember to take your Amusement IC card with you!";
                }

                _thanksForPlayingNotification = new Notification("Thanks for playing!", text, NotificationStyle.Okay, b => {
                    ResetLogin();

                    if (Director.CurrentOsuMode != OsuMode.MainMenu) {
                        Director.ChangeMode(OsuMode.MainMenu);
                    } else {
                        GameBase.Scheduler.Add(delegate {
                            MainMenu menu = (Director.CurrentMode as MainMenu);

                            //Shouldn't happen?
                            if (menu == null) {
                                return;
                            }

                            if (menu.State == MenuState.Select) {
                                menu.GoBackToOsuLogo();
                            }
                        }, 500);
                    }
                });

                _thanksForPlayingNotification.descriptionText.TextSize = 22;

                GameBase.Notify(_thanksForPlayingNotification);

                return true;
            }

            return false;
        }
    }
}
