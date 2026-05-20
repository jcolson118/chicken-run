using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "GroundTileRegistry", menuName = "Tilemaps/Ground Tile Registry")]
public class GroundTileRegistry : ScriptableObject
{
    [Header("Top Row - 4 variants (index-matched to Second Row)")]
    public TileBase[] topTiles = new TileBase[4];

    [Header("Second Row - must match Top Row variant by index")]
    public TileBase[] secondRowTiles = new TileBase[4];

    [Header("Left Edge - Top")]
    public TileBase elt;   // Edge Left Top
    public TileBase elst;  // Edge Left Second from Top

    [Header("Right Edge - Top")]
    public TileBase ert;   // Edge Right Top
    public TileBase erst;  // Edge Right Second from Top

    [Header("Left Edge - Filler (index 0 = ELF-1, index 1 = ELF-2)")]
    public TileBase[] elfTiles = new TileBase[2];

    [Header("Left Edge - Second from Edge Filler (must index-match elfTiles)")]
    public TileBase[] selfTiles = new TileBase[2];

    [Header("Right Edge - Filler (index 0 = ERF-1, index 1 = ERF-2)")]
    public TileBase[] erfTiles = new TileBase[2];

    [Header("Right Edge - Second from Edge Filler (must index-match erfTiles)")]
    public TileBase[] serfTiles = new TileBase[2];

    [Header("Generic Interior Filler - 4 variants, sampled randomly")]
    public TileBase[] fillerTiles = new TileBase[4];

    [Header("Left Edge - Bottom")]
    public TileBase elsb;  // Edge Left Second from Bottom
    public TileBase elb;   // Edge Left Bottom

    [Header("Right Edge - Bottom")]
    public TileBase ersb;  // Edge Right Second from Bottom
    public TileBase erb;   // Edge Right Bottom

    [Header("Bottom Row - Second from Left/Right")]
    public TileBase sbsl;  // Second to Bottom, Second from Left
    public TileBase bsl;   // Bottom, Second from Left
    public TileBase sbsr;  // Second to Bottom, Second from Right
    public TileBase bsr;   // Bottom, Second from Right

    [Header("Bottom Filler (index 0 = variant 1, index 1 = variant 2)")]
    public TileBase[] fsbTiles = new TileBase[2];  // Filler Second from Bottom
    public TileBase[] fbTiles = new TileBase[2];   // Filler Bottom (index-matched to fsbTiles)

    /// <summary>
    /// Validates that all tiles are assigned. Call this from the generator on startup.
    /// </summary>
    public bool Validate()
    {
        bool valid = true;

        valid &= ValidateArray(topTiles, "topTiles", 4);
        valid &= ValidateArray(secondRowTiles, "secondRowTiles", 4);
        valid &= ValidateTile(elt, "elt");
        valid &= ValidateTile(elst, "elst");
        valid &= ValidateTile(ert, "ert");
        valid &= ValidateTile(erst, "erst");
        valid &= ValidateArray(elfTiles, "elfTiles", 2);
        valid &= ValidateArray(selfTiles, "selfTiles", 2);
        valid &= ValidateArray(erfTiles, "erfTiles", 2);
        valid &= ValidateArray(serfTiles, "serfTiles", 2);
        valid &= ValidateArray(fillerTiles, "fillerTiles", 4);
        valid &= ValidateTile(elsb, "elsb");
        valid &= ValidateTile(elb, "elb");
        valid &= ValidateTile(ersb, "ersb");
        valid &= ValidateTile(erb, "erb");
        valid &= ValidateTile(sbsl, "sbsl");
        valid &= ValidateTile(bsl, "bsl");
        valid &= ValidateTile(sbsr, "sbsr");
        valid &= ValidateTile(bsr, "bsr");
        valid &= ValidateArray(fsbTiles, "fsbTiles", 2);
        valid &= ValidateArray(fbTiles, "fbTiles", 2);

        return valid;
    }

    private bool ValidateTile(TileBase tile, string name)
    {
        if (tile == null)
        {
            Debug.LogError($"[GroundTileRegistry] Missing tile: {name}");
            return false;
        }
        return true;
    }

    private bool ValidateArray(TileBase[] arr, string name, int expectedLength)
    {
        if (arr == null || arr.Length != expectedLength)
        {
            Debug.LogError($"[GroundTileRegistry] Array '{name}' must have {expectedLength} elements.");
            return false;
        }
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == null)
            {
                Debug.LogError($"[GroundTileRegistry] Missing tile in '{name}' at index {i}");
                return false;
            }
        }
        return true;
    }
}