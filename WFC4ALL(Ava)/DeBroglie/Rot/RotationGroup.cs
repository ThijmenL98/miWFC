using System;
using System.Collections;
using System.Collections.Generic;

namespace WFC4ALL.DeBroglie.Rot; 

/// <summary>
///     Describes a group of rotations and reflections.
/// </summary>
public class RotationGroup : IEnumerable<Rotation> {
    private readonly List<Rotation> rotations;

    public RotationGroup(int rotationalSymmetry, bool reflectionalSymmetry) {
        this.RotationalSymmetry = rotationalSymmetry;
        this.ReflectionalSymmetry = reflectionalSymmetry;
        SmallestAngle = 360 / rotationalSymmetry;
        rotations = new List<Rotation>();
        for (int refl = 0; refl < (reflectionalSymmetry ? 2 : 1); refl++) {
            for (int rot = 0; rot < 360; rot += SmallestAngle) {
                rotations.Add(new Rotation(rot, refl > 0));
            }
        }
    }

    /// <summary>
    ///     Indicates the number of distinct rotations in the group.
    /// </summary>
    public int RotationalSymmetry { get; }

    /// <summary>
    ///     If true, the group also contains reflections as well as rotations.
    /// </summary>
    public bool ReflectionalSymmetry { get; }

    /// <summary>
    ///     Defined as 360 / RotationalSymmetry, this is the the smallest angle of any rotation
    ///     in the group.
    /// </summary>
    public int SmallestAngle { get; }

    public IEnumerator<Rotation> GetEnumerator() {
        return rotations.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return rotations.GetEnumerator();
    }

    /// <summary>
    ///     Throws if rotation is not a member of the group.
    /// </summary>
    /// <param name="rotation"></param>
    public void CheckContains(Rotation rotation) {
        if (rotation.RotateCw / SmallestAngle * SmallestAngle != rotation.RotateCw) {
            throw new Exception($"Rotation angle {rotation.RotateCw} not permitted.");
        }

        if (rotation.ReflectX && !ReflectionalSymmetry) {
            throw new Exception("Reflections are not permitted.");
        }
    }
}