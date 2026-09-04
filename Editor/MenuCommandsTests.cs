using ClosingCircle.Domain;
using NUnit.Framework;
using System.Globalization;
using System.Threading;
using UnityEngine;

namespace ClosingCircle.Tests
{
    [TestFixture]
    public class MenuCommandsTests
    {
        [Test]
        public void SetWritesTheKeyTheConfigUses()
        {
            Assert.AreEqual("rc closingCircle set Opacity 40", MenuCommands.Set("Opacity", 40f));
            Assert.AreEqual("rc closingCircle set Solid true", MenuCommands.Set("Solid", true));
            Assert.AreEqual("rc closingCircle set Solid false", MenuCommands.Set("Solid", false));
            Assert.AreEqual("rc closingCircle set Shape Hexagon", MenuCommands.Set("Shape", "Hexagon"));
        }

        // Colour is three 0-255 channels on the wire, not a float or a hex string.
        [Test]
        public void ColourGoesOutAsThreeChannels()
        {
            Assert.AreEqual("rc closingCircle set Color 255,60,60",
                            MenuCommands.SetColour(new Color(1f, 60f / 255f, 60f / 255f)));

            Assert.AreEqual("rc closingCircle set Color 0,0,0", MenuCommands.SetColour(Color.black));
        }

        [Test]
        public void AModeReplacesTheCoordinatesRatherThanJoiningThem()
        {
            Assert.AreEqual("rc closingCircle stage add 540 480 100 Bisector",
                            MenuCommands.AddStage(540f, 480f, 100f, CentreMode.Bisector));

            Assert.AreEqual("rc closingCircle stage add 420 360 40 Random",
                            MenuCommands.AddStage(420f, 360f, 40f, CentreMode.Random));

            Assert.AreEqual("rc closingCircle stage add 540 480 100 30 20",
                            MenuCommands.AddStage(540f, 480f, 100f, new Vector2(30f, 20f)));
        }

        [Test]
        public void BisectorCarriesItsPointAndHeading()
        {
            Assert.AreEqual("rc closingCircle set Bisector 0,0,90",
                            MenuCommands.SetBisector(Vector2.zero, 90f));
        }

        [Test]
        public void PreviewTakesEitherASpanOrASwitch()
        {
            Assert.AreEqual("rc closingCircle preview 60", MenuCommands.Preview(60f));
            Assert.AreEqual("rc closingCircle preview on", MenuCommands.Preview(true));
            Assert.AreEqual("rc closingCircle preview off", MenuCommands.Preview(false));
        }

        [Test]
        public void TheRestAreWhatAnAdminWouldType()
        {
            Assert.AreEqual("rc closingCircle stage remove 2", MenuCommands.RemoveStage(2));
            Assert.AreEqual("rc closingCircle stage clear", MenuCommands.ClearStages());
            Assert.AreEqual("rc closingCircle stage next", MenuCommands.AdvanceStage());
            Assert.AreEqual("rc closingCircle validate", MenuCommands.Validate());
            Assert.AreEqual("rc closingCircle status", MenuCommands.Status());
            Assert.AreEqual("rc closingCircle push", MenuCommands.Push());
            Assert.AreEqual("rc closingCircle whoami", MenuCommands.Whoami());
            Assert.AreEqual("rc login hunter2", MenuCommands.Login("hunter2"));
        }

        // A client in a comma-decimal locale would otherwise send a comma into a comma-separated argument.
        [Test]
        public void NumbersAreInvariantWhateverTheClientLocaleIs()
        {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;

            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

                Assert.AreEqual("rc closingCircle set Fade 0.6", MenuCommands.Set("Fade", 0.6f));
                Assert.AreEqual("rc closingCircle set Bisector 12.5,-7.25,90",
                                MenuCommands.SetBisector(new Vector2(12.5f, -7.25f), 90f));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }
    }
}
