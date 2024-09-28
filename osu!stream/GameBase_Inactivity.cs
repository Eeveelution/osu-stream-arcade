using System;
using osum.GameModes;
using osum.GameModes.Play;
using osum.Input;
using osum.Input.Sources;
using osum.UI;

namespace osum {
    public partial class GameBase {
        private double       _inactivity;
        private Notification _inactivityPrompt;

        private void InputManagerActivityReset(InputSource source, TrackingPoint trackingPoint) {
            _inactivity = 0;
        }

        public void InitializeInactivity() {
            InputManager.OnDown += InputManagerActivityReset;
            InputManager.OnMove += InputManagerActivityReset;
            InputManager.OnUp   += InputManagerActivityReset;
        }

        public void LogOutFromInactivity() {
            ArcadeUserData.ResetLogin();

            if (Director.CurrentOsuMode != OsuMode.MainMenu) {
                Director.ChangeMode(OsuMode.MainMenu);
            }

            this._inactivityPrompt = null;
        }

        public void UpdateInactivity(double delta) {
            this._inactivity += delta;

            //If you're logged in, and haven't been active for over 20 seconds, bring up the prompt
            if (this._inactivity >= 20.0 && ArcadeUserData.HasAuth && (this._inactivityPrompt == null || this._inactivityPrompt.Dismissed)) {
                string description = (ArcadeUserData.HasAuth && !ArcadeUserData.IsGuest) ? ArcadeUserData.Username + " " : "";

                description += $"You have been inactive for quite a while, are you still playing?";

                _inactivityPrompt = new Notification(
                    "Are you still here?",
                    description,
                    NotificationStyle.YesNo,
                    delegate(bool here) {
                        if (!here) {
                            this.LogOutFromInactivity();
                        }

                        this._inactivity = 0;

                        if (this._inactivityPrompt != null) {
                            this._inactivityPrompt.Dismissed = true;
                        }
                    }
                );

                this._inactivityPrompt.yesText.Text = "I'm still here!";
                this._inactivityPrompt.noText.Text  = "Log me out!";

                Notify(_inactivityPrompt);

                Scheduler.Add(delegate {
                    if (this._inactivityPrompt != null && !this._inactivityPrompt.Dismissed) {
                        this._inactivityPrompt.Dismiss(false, false);

                        this.LogOutFromInactivity();
                    }
                }, 10000);
            }

            if (this._inactivity >= 30.0f && !ArcadeUserData.HasAuth && Director.CurrentOsuMode == OsuMode.MainMenu) {
                Tutorial.IsDemoMode = true;

                Director.ChangeMode(OsuMode.Tutorial);
            }
        }
    }
}
