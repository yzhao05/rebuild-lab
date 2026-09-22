using System;
using EMVR.ReusableFoundation.Gate5.Editor;
using NUnit.Framework;

namespace EMVR.ReusableFoundation.Lighting.Tests
{
    public sealed class EmVrRoomLightingTests
    {
        [Test]
        public void IndependentLabsUseIndependentExecutionAssets()
        {
            var first = EmVrRoomLighting.ForLab("lab_one", "Assets/Scenes/lab_one.unity");
            var second = EmVrRoomLighting.ForLab("lab_two", "Assets/Scenes/lab_two.unity");
            Assert.AreNotEqual(first.ExecutionPath, second.ExecutionPath);
            Assert.AreEqual("Assets/Generated/lab_two/ApprovedDependencies/EMVRRoom/Lighting/RoomLighting.lighting", second.SourceLightingPath);
        }

        [Test]
        public void TraversalAndNativePathsAreRejected()
        {
            Assert.AreEqual("Assets/Scenes/电场演示/Lab One.unity",
                EmVrRoomLighting.ForLab("lab", "Assets/Scenes/电场演示/Lab One.unity").OutputScenePath);
            Assert.Throws<ArgumentException>(() => EmVrRoomLighting.ForLab("../other", "Assets/Scenes/lab.unity"));
            Assert.Throws<ArgumentException>(() => EmVrRoomLighting.ForLab("lab", "Assets/Scenes/../other.unity"));
            Assert.Throws<ArgumentException>(() => EmVrRoomLighting.ForLab("lab", @"Assets\Scenes\lab.unity"));
        }

        [Test]
        public void SaveGuardProtectsEveryLabButNotExecutionOrUnrelatedAssets()
        {
            var first = EmVrRoomLighting.ForLab("lab_one", "Assets/Scenes/lab_one.unity");
            var second = EmVrRoomLighting.ForLab("lab_two", "Assets/Scenes/lab_two.unity");
            string unrelated = "Assets/Other/RoomLighting.lighting";
            CollectionAssert.AreEqual(new[] { first.ExecutionPath, unrelated, first.SourceLightingPath + ".backup" },
                EmVrRoomLighting.FilterSavePaths(new[] { first.SourceLightingPath, second.SourceLightingPath + ".meta",
                    first.SourceLightingPath.ToUpperInvariant().Replace('/', '\\'), first.ExecutionPath, unrelated, first.SourceLightingPath + ".backup" }));
        }

        [Test]
        public void NativeIdentityAndEquivalentNumericSerializationAreAllowed()
        {
            EmVrRoomLighting.ValidateLightingFields("  m_Name: Source\n  serializedVersion: 3\n  m_Value: 1.0\n",
                "  m_Name: RoomLighting_Execution\n  serializedVersion: 4\n  m_Value: 1\n");
        }

        [Test]
        public void ChangedBrightnessOrAutomaticGenerationIsRejected()
        {
            const string original = "  m_Intensity: 5\n  m_GIWorkflowMode: 1\n";
            Assert.Throws<InvalidOperationException>(() => EmVrRoomLighting.ValidateLightingFields(original, original.Replace("5", "6")));
            Assert.Throws<InvalidOperationException>(() => EmVrRoomLighting.ValidateLightingFields(original, original.Replace("Mode: 1", "Mode: 0")));
        }

        [Test]
        public void AssignmentRejectsImportedOriginalOtherLabAndWrongMode()
        {
            var lighting = EmVrRoomLighting.ForLab("lab_one", "Assets/Scenes/lab_one.unity");
            lighting.RequireExecutionBinding(lighting.ExecutionPath, true, false, false);
            Assert.Throws<InvalidOperationException>(() => lighting.RequireExecutionBinding(lighting.SourceLightingPath, true, false, false));
            Assert.Throws<InvalidOperationException>(() => lighting.RequireExecutionBinding("Assets/Scenes/lab_two/RoomLighting_Execution.lighting", true, false, false));
            Assert.Throws<InvalidOperationException>(() => lighting.RequireExecutionBinding(lighting.ExecutionPath, true, false, true));
            Assert.Throws<InvalidOperationException>(() => lighting.RequireExecutionBinding(lighting.ExecutionPath, false, true, false));
        }

        [Test]
        public void MissingSettingsCannotPassAnEmptyComparison()
        {
            Assert.Throws<InvalidOperationException>(() => EmVrRoomLighting.ValidateLightingFields("", ""));
            Assert.Throws<InvalidOperationException>(() => EmVrRoomLighting.ValidateLightingFields("  m_Intensity: 5\n", "  m_Name: x\n"));
        }
    }
}
