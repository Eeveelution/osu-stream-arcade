using System;
using System.Diagnostics;
using osum.GameModes;
using osum.GameModes.MainMenu;
using osum.Helpers;
using osum.UI;

namespace osum {
    public class ArcadeUserData {
        private const int CreditLength = 10 * 60 * 1000;
        // private const int CreditLength = 15000;

        public static bool      HasAuth       = false;
        public static bool      IsGuest       = true;
        public static string    SubmitToken   = "";
        public static string    Username      = "";
        public static ulong     UserId        = 0;
        public static double    StatStreams   = 0.0;
        public static Stopwatch CreditCounter = new Stopwatch();

        private static Notification _thanksForPlayingNotification;

        public static string GetFormattedRemainingTime() {
            long remainingSeconds = (long)( (double)(CreditLength - CreditCounter.ElapsedMilliseconds) / 1000.0f);

            double minutes = Math.Max(Math.Floor((double) remainingSeconds / 60.0f), 0);

            remainingSeconds -= (long) (minutes * 60.0f);

            return $"{(int) minutes}.{(int)Math.Max(remainingSeconds, 0):00}";
        }

        public static void StartCredit() {
            CreditCounter.Start();

            if (IsGuest) {
                HasAuth  = true;
                Username = "Guest";
            }
        }

        public static bool CreditStarted() {
            return CreditCounter.ElapsedMilliseconds > 0;
        }

        public static bool CreditOver() {
            return CreditCounter.ElapsedMilliseconds >= CreditLength;
        }

        public static void ResetLogin() {
            HasAuth       = false;
            IsGuest       = true;
            SubmitToken    = "";
            Username      = "";
            UserId        = 0;
            StatStreams   = 0.0;
            CreditCounter = new Stopwatch();
        }

        public static bool CreditOverReturnCatch(VoidDelegate preQuitTask = null) {
            if (CreditStarted() && CreditOver() && (_thanksForPlayingNotification == null || _thanksForPlayingNotification.Dismissed) ) {
                if (preQuitTask != null) {
                    preQuitTask();
                }

                string text = "Your timer has run out! Please look around and let others try out the arcade too!";

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
