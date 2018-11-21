using System;
using System.Globalization;
using System.Windows;
using DataBaseCompare.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CopyDatabaseTests {
    [TestClass]
    public class BooleanToVisibilityConverterTests {
        private BooleanToVisibilityConverter converter;

        public BooleanToVisibilityConverterTests() {
            this.converter = new BooleanToVisibilityConverter();
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
    }
}
