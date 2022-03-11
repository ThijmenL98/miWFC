using System;
using System.Windows.Forms;
using WFC4All.Properties;

namespace WFC4All {
    public static class ButtonVisualEventHandler {
        public static void animateButton_MouseEnter(object sender, EventArgs e) {
            if (((Button) sender).BackgroundImage.Tag == null 
                || ((Button) sender).BackgroundImage.Tag.Equals("Animate")) {
                ((Button) sender).BackgroundImage = Resources.AnimateHover;
                ((Button) sender).BackgroundImage.Tag = "Animate";
            } else {
                ((Button) sender).BackgroundImage = Resources.PauseHover;
                ((Button) sender).BackgroundImage.Tag = "Pause";
            }
        }

        public static void animateButton_MouseLeave(object sender, EventArgs e) {
            if (((Button) sender).BackgroundImage.Tag == null 
                || ((Button) sender).BackgroundImage.Tag.Equals("Animate")) {
                ((Button) sender).BackgroundImage = Resources.Animate;
                ((Button) sender).BackgroundImage.Tag = "Animate";
            } else {
                ((Button) sender).BackgroundImage = Resources.Pause;
                ((Button) sender).BackgroundImage.Tag = "Pause";
            }
        }

        public static void animateButton_MouseDown(object sender, MouseEventArgs e) {
            if (((Button) sender).BackgroundImage.Tag == null 
                || ((Button) sender).BackgroundImage.Tag.Equals("Animate")) {
                ((Button) sender).BackgroundImage = Resources.AnimateClick;
                ((Button) sender).BackgroundImage.Tag = "Animate";
            } else {
                ((Button) sender).BackgroundImage = Resources.PauseClick;
                ((Button) sender).BackgroundImage.Tag = "Pause";
            }
        }

        public static void backButton_MouseEnter(object sender, EventArgs e) {
            ((Button) sender).BackgroundImage = Resources.RevertHover;
        }

        public static void backButton_MouseLeave(object sender, EventArgs e) {
            ((Button) sender).BackgroundImage = Resources.Revert;
        }

        public static void backButton_MouseDown(object sender, MouseEventArgs e) {
            ((Button) sender).BackgroundImage = Resources.RevertClick;
        }

        public static void advanceButton_MouseDown(object sender, MouseEventArgs e) {
            ((Button) sender).BackgroundImage = Resources.AdvanceClick;
        }

        public static void advanceButton_MouseEnter(object sender, EventArgs e) {
            ((Button) sender).BackgroundImage = Resources.AdvanceHover;
        }

        public static void advanceButton_MouseLeave(object sender, EventArgs e) {
            ((Button) sender).BackgroundImage = Resources.Advance;
        }

        public static void markerButton_MouseDown(object sender, MouseEventArgs e) {
            ((Button) sender).BackgroundImage = Resources.SaveClick;
        }

        public static void markerButton_MouseEnter(object sender, EventArgs e) {
            ((Button) sender).BackgroundImage = Resources.SaveHover;
        }

        public static void markerButton_MouseLeave(object sender, EventArgs e) {
            ((Button) sender).BackgroundImage = Resources.Save;
        }

        public static void revertMarkerButton_MouseDown(object sender, MouseEventArgs e) {
            ((Button) sender).BackgroundImage = Resources.LoadClick;
        }

        public static void revertMarkerButton_MouseEnter(object sender, EventArgs e) {
            ((Button) sender).BackgroundImage = Resources.LoadHover;
        }

        public static void revertMarkerButton_MouseLeave(object sender, EventArgs e) {
            ((Button) sender).BackgroundImage = Resources.Load;
        }

        public static void backButton_MouseUp(object sender, MouseEventArgs e) {
            ((Button) sender).BackgroundImage = Resources.RevertHover;
        }

        public static void advanceButton_MouseUp(object sender, MouseEventArgs e) {
            ((Button) sender).BackgroundImage = Resources.AdvanceHover;
        }

        public static void revertMarkerButton_MouseUp(object sender, MouseEventArgs e) {
            ((Button) sender).BackgroundImage = Resources.LoadHover;
        }

        public static void markerButton_MouseUp(object sender, MouseEventArgs e) {
            ((Button) sender).BackgroundImage = Resources.SaveHover;
        }
        public static void restartButton_MouseDown(object sender, MouseEventArgs e) {
            ((Button) sender).BackgroundImage = Resources.RestartClick;
        }

        public static void restartButton_MouseEnter(object sender, EventArgs e) {
            ((Button) sender).BackgroundImage = Resources.RestartHover;
        }

        public static void restartButton_MouseLeave(object sender, EventArgs e) {
            ((Button) sender).BackgroundImage = Resources.Restart;
        }

        public static void restartButton_MouseUp(object sender, MouseEventArgs e) {
            ((Button) sender).BackgroundImage = Resources.RestartHover;
        }
        
        public static void infoButton_MouseDown(object sender, MouseEventArgs e) {
            ((Button) sender).BackgroundImage = Resources.InfoClick;
        }

        public static void infoButton_MouseEnter(object sender, EventArgs e) {
            ((Button) sender).BackgroundImage = Resources.InfoHover;
        }

        public static void infoButton_MouseLeave(object sender, EventArgs e) {
            ((Button) sender).BackgroundImage = Resources.Info;
        }

        public static void infoButton_MouseUp(object sender, MouseEventArgs e) {
            ((Button) sender).BackgroundImage = Resources.InfoHover;
        }
        
        public static void closeButton_MouseDown(object sender, MouseEventArgs e) {
            ((Button) sender).BackgroundImage = Resources.CloseClick;
        }

        public static void closeButton_MouseEnter(object sender, EventArgs e) {
            ((Button) sender).BackgroundImage = Resources.CloseHover;
        }

        public static void closeButton_MouseLeave(object sender, EventArgs e) {
            ((Button) sender).BackgroundImage = Resources.Close;
        }

        public static void closeButton_MouseUp(object sender, MouseEventArgs e) {
            ((Button) sender).BackgroundImage = Resources.CloseHover;
        }
        
        public static void exportButton_MouseDown(object sender, MouseEventArgs e) {
            ((Button) sender).BackgroundImage = Resources.CloseClick; //TODO
        }

        public static void exportButton_MouseEnter(object sender, EventArgs e) {
            ((Button) sender).BackgroundImage = Resources.CloseHover; //TODO
        }

        public static void exportButton_MouseLeave(object sender, EventArgs e) {
            ((Button) sender).BackgroundImage = Resources.Close; //TODO
        }

        public static void exportButton_MouseUp(object sender, MouseEventArgs e) {
            ((Button) sender).BackgroundImage = Resources.CloseHover; //TODO
        }
    }
}