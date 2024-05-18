namespace DeepNestLib
{
    public enum PlacementType
    {
        BoundingBox,
        Gravity,
        Squeeze
    }

    public record NestingConfigurations
    {
        public static NestingConfigurations Default { get; } = new NestingConfigurations()
        {
            PlacementType = PlacementType.BoundingBox,
            CurveTolerance = 0.72,
            Scale = 25,
            ClipperScale = 10000000,
            ExploreConcave = false,
            MutationRate = 10,
            PopulationSize = 10,
            Rotations = 4,
            Spacing = 14,
            SheetSpacing = 5,
            UseHoles = false,
            TimeRatio = 0.5,
            MergeLines = false,
            SimplifyGeometry = true,
            UseParallelProcessing = true,
        };

        public PlacementType PlacementType = PlacementType.BoundingBox;
        public double CurveTolerance = 0.72;
        public double Scale = 25;
        public double ClipperScale = 10000000;
        public bool ExploreConcave = false;
        public int MutationRate = 10;
        public int PopulationSize = 10;
        public int Rotations = 4;
        public double Spacing = 14;
        public double SheetSpacing = 5;
        public bool UseHoles = false;
        public double TimeRatio = 0.5;
        public bool MergeLines = false;
        public bool SimplifyGeometry = true;
        public bool UseParallelProcessing = true;
    }
}