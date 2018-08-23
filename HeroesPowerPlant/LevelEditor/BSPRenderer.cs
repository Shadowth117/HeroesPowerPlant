﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using SharpDX;
using HeroesONE_R.Structures;
using HeroesONE_R.Structures.Subsctructures;
using RenderWareFile;
using static HeroesPowerPlant.SharpRenderer;

namespace HeroesPowerPlant
{
    public class BSPRenderer
    {
        public static void Dispose()
        {
            foreach (RenderWareModelFile r in BSPList)
                foreach (SharpMesh mesh in r.meshList)
                    mesh.Dispose();

            foreach (RenderWareModelFile r in ShadowColBSPList)
                foreach (SharpMesh mesh in r.meshList)
                    mesh.Dispose();
        }

        public static string currentFileNamePrefix = "default";
        public static List<RenderWareModelFile> BSPList = new List<RenderWareModelFile>();

        public static void SetHeroesBSPList(Archive heroesONEfile)
        {
            Dispose();
            ReadFileMethods.isShadow = false;

            BSPList = new List<RenderWareModelFile>(heroesONEfile.Files.Count);
            ShadowColBSPList = new List<RenderWareModelFile>();

            foreach (ArchiveFile file in heroesONEfile.Files)
            {
                if (!(new string[] { ".bsp", ".rg1", ".rx1" }.Contains(Path.GetExtension(file.Name).ToLower())))
                    continue;
                
                RenderWareModelFile TempBSPFile = new RenderWareModelFile(file.Name);
                TempBSPFile.SetChunkNumberAndName();
                byte[] uncompressedData = file.DecompressThis();
                TempBSPFile.SetForRendering(ReadFileMethods.ReadRenderWareFile(uncompressedData), uncompressedData);
                BSPList.Add(TempBSPFile);
            }
        }
        
        // Visibility functions

        private static HashSet<int> VisibleChunks = new HashSet<int>();

        public static void DetermineVisibleChunks()
        {
            VisibleChunks.Clear();
            VisibleChunks.Add(-1);
            Vector3 cameraPos = Camera.GetPosition();

            foreach (LevelEditor.Chunk c in LevelEditor.VisibilityFunctions.ChunkList)
            {
                if ((cameraPos.X > c.Min.X) & (cameraPos.Y > c.Min.Y) & (cameraPos.Z > c.Min.Z) &
                    (cameraPos.X < c.Max.X) & (cameraPos.Y < c.Max.Y) & (cameraPos.Z < c.Max.Z))
                {
                    VisibleChunks.Add(c.number);
                }
            }
        }

        public static bool renderByChunk = true;
                
        // Rendering functions
        
        public static void RenderLevelModel(Matrix viewProjection)
        {
            if (renderByChunk)
                DetermineVisibleChunks();

            device.SetFillModeDefault();
            defaultShader.Apply();

            RenderOpaque(viewProjection);
            RenderAlpha(viewProjection);
        }

        private static void RenderOpaque(Matrix viewProjection)
        {
            device.SetDefaultBlendState();
            device.SetDefaultDepthState();
            device.SetCullModeDefault();

            device.UpdateData(defaultBuffer, viewProjection);
            device.DeviceContext.VertexShader.SetConstantBuffer(0, defaultBuffer);

            for (int j = 0; j < BSPList.Count; j++)
            {
                if ((renderByChunk & !VisibleChunks.Contains(BSPList[j].ChunkNumber)) |
                    (BSPList[j].ChunkName == "A" | BSPList[j].ChunkName == "P" | BSPList[j].ChunkName == "K"))
                    continue;

                if (BSPList[j].isNoCulling) device.SetCullModeNone();
                else device.SetCullModeDefault();

                device.ApplyRasterState();
                device.UpdateAllStates();

                BSPList[j].Render();
            }
        }

        private static void RenderAlpha(Matrix viewProjection)
        {
            for (int j = 0; j < BSPList.Count; j++)
            {
                if ((renderByChunk & !VisibleChunks.Contains(BSPList[j].ChunkNumber)) |
                    (BSPList[j].ChunkName == "O"))
                    continue;

                if (BSPList[j].isNoCulling) device.SetCullModeNone();
                else device.SetCullModeDefault();

                if (BSPList[j].ChunkName == "A" | BSPList[j].ChunkName == "P")
                {
                    device.SetBlendStateAlphaBlend();
                }
                else if (BSPList[j].ChunkName == "K")
                {
                    device.SetBlendStateAdditive();
                }

                device.ApplyRasterState();
                device.UpdateAllStates();

                device.UpdateData(defaultBuffer, viewProjection);
                device.DeviceContext.VertexShader.SetConstantBuffer(0, defaultBuffer);

                BSPList[j].Render();
            }
        }

        // Shadow functions
        public static string currentShadowFolderNamePrefix = "default";

        public static void LoadShadowLevelFolder(string Folder)
        {
            List<Archive> ShadowONEFiles = new List<Archive>();
            currentShadowFolderNamePrefix = Path.GetFileNameWithoutExtension(Folder);

            foreach (string fileName in Directory.GetFiles(Folder))
            {
                if (Path.GetExtension(fileName).ToLower() == ".one")
                    if (!(fileName.Contains("dat") |
                        fileName.Contains("fx") |
                        fileName.Contains("gdt") |
                        fileName.Contains("tex")))
                    {
                        byte[] oneDataBytes = File.ReadAllBytes(fileName);
                        ShadowONEFiles.Add(Archive.FromONEFile(ref oneDataBytes));
                    }
                    else if (fileName.Contains("dat"))
                    {
                        Program.LevelEditor.initVisibilityEditor(true, fileName);
                    }
                    else if (fileName.Contains("fx"))
                    {
                        //  OpenShadowFXONE = new HeroesONEFile(fileName);
                    }
                    else if (fileName.Contains("gdt"))
                    {
                        // OpenShadowGDTONE = new HeroesONEFile(fileName);
                    }
                    else if (fileName.Contains("tex"))
                    {
                        // OpenShadowTexONE = new HeroesONEFile(fileName);
                    }
            }

            SetShadowBSPList(ShadowONEFiles);
        }

        public static List<RenderWareModelFile> ShadowColBSPList = new List<RenderWareModelFile>();

        private static void SetShadowBSPList(List<Archive> OpenShadowONEFiles)
        {
            Dispose();
            
            BSPList = new List<RenderWareModelFile>();
            ShadowColBSPList = new List<RenderWareModelFile>();

            ReadFileMethods.isShadow = true;

            foreach (Archive f in OpenShadowONEFiles)
                foreach (ArchiveFile file in f.Files)
                {
                    string ChunkName = Path.GetFileNameWithoutExtension(file.Name);

                    if (ChunkName.Contains("COLI"))
                    {
                        ReadFileMethods.isCollision = true;

                        RenderWareModelFile TempBSPFile = new RenderWareModelFile(file.Name);
                        try
                        {
                            TempBSPFile.ChunkNumber = Convert.ToByte(ChunkName.Split('_').Last());
                        }
                        catch { TempBSPFile.ChunkNumber = -1; };

                        TempBSPFile.isShadowCollision = true;
                        try
                        {
                            byte[] data = file.DecompressThis();
                            TempBSPFile.SetForRendering(ReadFileMethods.ReadRenderWareFile(data), data);
                        }
                        catch (Exception e)
                        {
                            System.Windows.Forms.MessageBox.Show("Error on opening " + file.Name + ": " + e.Message);
                        }
                        ShadowColBSPList.Add(TempBSPFile);

                        ReadFileMethods.isCollision = false;
                    }
                    else
                    {
                        RenderWareModelFile TempBSPFile = new RenderWareModelFile(file.Name);
                        TempBSPFile.SetChunkNumberAndName();
                        byte[] data = file.DecompressThis();
                        TempBSPFile.SetForRendering(ReadFileMethods.ReadRenderWareFile(data), data);
                        BSPList.Add(TempBSPFile);
                    }
                }
        }

        public static void RenderShadowCollisionModel(Matrix viewProjection)
        {
            if (renderByChunk)
                DetermineVisibleChunks();

            device.SetDefaultBlendState();
            device.SetFillModeDefault();
            device.SetCullModeDefault();
            device.ApplyRasterState();
            device.UpdateAllStates();

            device.UpdateData(defaultBuffer, viewProjection);
            device.DeviceContext.VertexShader.SetConstantBuffer(0, defaultBuffer);
            defaultShader.Apply();
                        
            for (int j = 0; j < ShadowColBSPList.Count; j++)
            {
                if (renderByChunk & !VisibleChunks.Contains(ShadowColBSPList[j].ChunkNumber))
                    continue;

                ShadowColBSPList[j].Render();
            }
        }
    }
}
