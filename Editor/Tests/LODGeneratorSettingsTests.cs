using NUnit.Framework;

namespace Plugins.AutoLODGenerator.Editor.Tests
{
    public class LODGeneratorSettingsTests
    {
        [Test]
        public void DefaultSettings_HasValidLODLevelCount()
        {
            var settings = new LODGeneratorSettings();

            Assert.GreaterOrEqual(settings.lodLevelCount, LODGeneratorSettings.MinLODLevels);
            Assert.LessOrEqual(settings.lodLevelCount, LODGeneratorSettings.MaxLODLevels);
        }

        [Test]
        public void DefaultSettings_QualityFactorsStartAtOne()
        {
            var settings = new LODGeneratorSettings();

            Assert.AreEqual(1.0f, settings.qualityFactors[0],
                "LOD0 quality factor should always be 1.0 (original mesh)");
        }

        [Test]
        public void DefaultSettings_QualityFactorsAreDescending()
        {
            var settings = new LODGeneratorSettings();

            for (int i = 1; i < settings.lodLevelCount; i++)
            {
                Assert.Less(settings.qualityFactors[i], settings.qualityFactors[i - 1],
                    $"Quality factor at LOD{i} should be less than LOD{i - 1}");
            }
        }

        [TestCase(LODPreset.Performance)]
        [TestCase(LODPreset.Balanced)]
        [TestCase(LODPreset.Quality)]
        [TestCase(LODPreset.MobileLowEnd)]
        [TestCase(LODPreset.MobileHighEnd)]
        [TestCase(LODPreset.VR)]
        public void ApplyPreset_SetsValidConfiguration(LODPreset preset)
        {
            var settings = new LODGeneratorSettings();
            settings.ApplyPreset(preset);

            Assert.GreaterOrEqual(settings.lodLevelCount, LODGeneratorSettings.MinLODLevels);
            Assert.LessOrEqual(settings.lodLevelCount, LODGeneratorSettings.MaxLODLevels);
            Assert.AreEqual(1.0f, settings.qualityFactors[0],
                "LOD0 should always be 1.0 after preset application");
        }

        [TestCase(LODPreset.Performance)]
        [TestCase(LODPreset.Balanced)]
        [TestCase(LODPreset.Quality)]
        [TestCase(LODPreset.MobileLowEnd)]
        [TestCase(LODPreset.MobileHighEnd)]
        [TestCase(LODPreset.VR)]
        public void ApplyPreset_ScreenHeightsAreDescending(LODPreset preset)
        {
            var settings = new LODGeneratorSettings();
            settings.ApplyPreset(preset);

            for (int i = 1; i < settings.lodLevelCount; i++)
            {
                Assert.Less(settings.screenTransitionHeights[i], settings.screenTransitionHeights[i - 1],
                    $"Screen height at LOD{i} should be less than LOD{i - 1}");
            }
        }
    }
}
