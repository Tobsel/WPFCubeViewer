using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Text;

namespace WPFCubeViewer.Interfaces
{
    /// <summary>
    /// Interface for voxel scripts used to evaluate voxel colors.
    /// </summary>
    public interface IVoxelScript
    {
        int Eval(int x, int y, int z, int t, int extent);
    }
}
