﻿using HeroesPowerPlant.Shared.Utilities;
using SharpDX;

namespace HeroesPowerPlant.LayoutEditor
{
    public class Object0180_FlowerPatch : SetObjectHeroes
    {
        public override void CreateTransformMatrix()
        {
            transformMatrix = Matrix.Scaling(Scale) * DefaultTransformMatrix();

            CreateBoundingBox();
        }

        public byte ObjectType { get; set; }
        public float Scale { get; set; }

        public override void ReadMiscSettings(EndianBinaryReader reader)
        {
            ObjectType = reader.ReadByte();
            reader.BaseStream.Position += 3;
            Scale = reader.ReadSingle();
        }

        public override void WriteMiscSettings(EndianBinaryWriter writer)
        {
            writer.Write(ObjectType);
            writer.Pad(3);
            writer.Write(Scale);
        }
    }
}
