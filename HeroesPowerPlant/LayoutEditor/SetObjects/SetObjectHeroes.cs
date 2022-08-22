﻿using HeroesPowerPlant.Shared.Utilities;
using Newtonsoft.Json;
using SharpDX;
using System;
using System.IO;
using System.Windows.Forms;

namespace HeroesPowerPlant.LayoutEditor
{
    public abstract class SetObjectHeroes : SetObject
    {
        [JsonConstructor]
        public SetObjectHeroes()
        {
            UnkBytes = new byte[8];
        }

        public abstract void ReadMiscSettings(EndianBinaryReader reader);

        public abstract void WriteMiscSettings(EndianBinaryWriter writer);

        public override byte[] GetMiscSettings()
        {
            using var writer = new EndianBinaryWriter(new MemoryStream(), Endianness.Big);
            WriteMiscSettings(writer);
            return ((MemoryStream)writer.BaseStream).ToArray();
        }

        public override void CreateTransformMatrix()
        {
            transformMatrix = DefaultTransformMatrix();
            CreateBoundingBox();
        }

        public Matrix DefaultTransformMatrix(float yAdd = 0) =>
            Matrix.RotationY(ReadWriteCommon.BAMStoRadians((int)Rotation.Y) + yAdd) *
            Matrix.RotationX(ReadWriteCommon.BAMStoRadians((int)Rotation.X)) *
            Matrix.RotationZ(ReadWriteCommon.BAMStoRadians((int)Rotation.Z)) *
            Matrix.Translation(Position);
    }
}

