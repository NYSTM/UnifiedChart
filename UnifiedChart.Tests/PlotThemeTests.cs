namespace UnifiedChart.Tests
{
    using UnifiedChart;
    using Xunit;

    public class PlotThemeTests
    {
        [Fact]
        public void Light_HasWhiteBackgroundAndBlackText()
        {
            var theme = PlotTheme.Light;

            Assert.Equal("White", theme.BackgroundColor);
            Assert.Equal("Black", theme.TitleColor);
            Assert.Equal("Black", theme.AxisLabelColor);
            Assert.Equal("Black", theme.LegendTextColor);
            Assert.NotEmpty(theme.SeriesPalette);
        }

        [Fact]
        public void Dark_HasDarkBackgroundAndLightText()
        {
            var theme = PlotTheme.Dark;

            Assert.Equal("#FF1E1E1E", theme.BackgroundColor);
            Assert.Equal("White", theme.TitleColor);
            Assert.NotEmpty(theme.SeriesPalette);
        }

        [Fact]
        public void Colorful_HasVividPalette()
        {
            var theme = PlotTheme.Colorful;

            Assert.Equal("White", theme.BackgroundColor);
            Assert.NotEmpty(theme.SeriesPalette);
            Assert.NotEqual(PlotTheme.Light.SeriesPalette, theme.SeriesPalette);
        }

        [Fact]
        public void PlotModel_DefaultsToLightTheme()
        {
            var model = new PlotModel();

            Assert.Same(PlotTheme.Light, model.Theme);
        }

        [Fact]
        public void PlotModel_Theme_CanBeChangedToDark()
        {
            var model = new PlotModel { Theme = PlotTheme.Dark };

            Assert.Same(PlotTheme.Dark, model.Theme);
        }

        [Fact]
        public void CustomTheme_PropertiesCanBeOverridden()
        {
            var theme = new PlotTheme
            {
                BackgroundColor = "#FF000000",
                SeriesPalette = new[] { "Red", "Green" }
            };

            Assert.Equal("#FF000000", theme.BackgroundColor);
            Assert.Equal(2, theme.SeriesPalette.Length);
        }
    }
}
