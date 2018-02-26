namespace jksharp.jklviewer.JKL
{
    internal class Material
    {
        public string Name { get; set; }
        public float XTile { get; set; }
        public float YTile { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}