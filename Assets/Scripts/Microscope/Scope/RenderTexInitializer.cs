using UnityEngine;
using System.Collections;

/// <summary>
/// This class handles initialization of a rendertexture (in this case, a lens texture)
/// </summary>
public class RenderTexInitializer : MonoBehaviour {

    public int TextureSize; //Note - this MUST be a power of 2

    public enum AASampleCount {
        _8x,
        _4x,
        _2x,
        _0x
    };
    public AASampleCount Antialiasing; //Texture antialiasing sample count

    void Start() {
        RenderTexture tex = gameObject.GetComponent<RenderTexture>();
        if (tex == null) return;

        //Set AA sample count (inspector value)
        switch (Antialiasing) {
            case AASampleCount._8x:
                tex.antiAliasing = 8;
                break;
            case AASampleCount._4x:
                tex.antiAliasing = 4;
                break;
            case AASampleCount._2x:
                tex.antiAliasing = 2;
                break;
            case AASampleCount._0x:
                tex.antiAliasing = 0;
                break;
            default:
                tex.antiAliasing = 0;
                break;
        }

        //Set texture size
        if (CheckTexSizeValidity(TextureSize)) {
            tex.width = TextureSize;
            tex.height = TextureSize;
        }

    }

    //Ensure given texture size is a power of two and thus valid
    private bool CheckTexSizeValidity(int size) {
        if (size == 0) return true;
        return ((size & (size - 1)) == 0);
    }

}
