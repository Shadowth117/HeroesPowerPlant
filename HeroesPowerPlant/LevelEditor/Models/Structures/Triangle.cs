﻿namespace HeroesPowerPlant.LevelEditor
{
    public class Triangle
    {
        public int MaterialIndex;

        public int vertex1;
        public int vertex2;
        public int vertex3;

        public int UVCoord1;
        public int UVCoord2;
        public int UVCoord3;

        public int Color1;
        public int Color2;
        public int Color3;
    }

    public class TriangleExt : Triangle
    {
        public RenderWareFile.Color collisionFlag;
    }

}