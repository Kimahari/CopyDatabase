using DataBaseCompare.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Windows;

namespace CopyDatabaseTests {

    [TestClass]
    public class BooleanToVisibilityConverterTests {

        #region Private Fields

        private readonly BooleanToVisibilityConverter converter;

        #endregion Private Fields

        #region Public Constructors

        public BooleanToVisibilityConverterTests() {
            converter = new BooleanToVisibilityConverter();
        }

        #endregion Public Constructors

        #region Public Methods

        [TestMethod]
        public void MustBeCollapsed() {
            Assert.AreEqual(Visibility.Collapsed, converter.Convert(false, null, null, CultureInfo.InvariantCulture));
        }

        [TestMethod]
        public void MustBeCollapsedReverse() {
            converter.TriggerValue = true;
            Assert.AreEqual(Visibility.Collapsed, converter.Convert(true, null, null, CultureInfo.InvariantCulture));
            converter.TriggerValue = false;
        }

        [TestMethod]
        public void MustBeHidden() {
            converter.IsHidden = true;
            Assert.AreEqual(Visibility.Hidden, converter.Convert(false, null, null, CultureInfo.InvariantCulture));
            converter.IsHidden = false;
        }

        [TestMethod]
        public void MustBeHiddenReverse() {
            converter.IsHidden = true;
            converter.TriggerValue = true;
            Assert.AreEqual(Visibility.Hidden, converter.Convert(true, null, null, CultureInfo.InvariantCulture));
            converter.IsHidden = false;
            converter.TriggerValue = false;
        }

        [TestMethod]
        public void MustBeVisible() {
            Assert.AreEqual(Visibility.Visible, converter.Convert(true, null, null, CultureInfo.InvariantCulture));
        }

        [TestMethod]
        public void MustBeVisibleReverse() {
            converter.TriggerValue = true;
            Assert.AreEqual(Visibility.Visible, converter.Convert(false, null, null, CultureInfo.InvariantCulture));
            converter.TriggerValue = false;
        }

        #endregion Public Methods
    }
}
