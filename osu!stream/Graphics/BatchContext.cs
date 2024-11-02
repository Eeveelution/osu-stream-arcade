using System.Collections.Generic;
using System.Drawing;
using OpenTK;

namespace osum.Graphics {
    public enum BatchType {
        Sprites,
        ApproachCircles,
        SliderBodies
    }
    public static class BatchContext {
        /// <summary>
        /// Whether there's currently a Batch ongoing.
        /// </summary>
        public static bool BatchPending { get; private set; }
        /// <summary>
        /// What is currently being Batched
        /// </summary>
        public static BatchType BatchType { get; private set; }

        public static Matrix4 ProjectionMatrix;
        public static Matrix4 TranslationMatrix;

        public static void Initialize() {
            BatchPending = false;
            BatchType    = BatchType.Sprites;

            ProjectionMatrix = Matrix4.Identity;
            TranslationMatrix = Matrix4.Identity;
        }

        public static void BeginBatch(BatchType type) {
            BatchPending = true;
            BatchType    = type;
        }

        public static void Restart() {

        }
    }
}
