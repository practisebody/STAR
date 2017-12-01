using UnityEngine;
using UnityEngine.UI;

public class VideoPreview : MonoBehaviour
{
    protected RawImage RawImage;
    public Texture2D Texture
    {
        get { return RawImage.texture as Texture2D; }
        protected set { RawImage.texture = value; }
    }

    private void Start()
    {
        RawImage = GetComponent<RawImage>();
    }

    public void SetResolution(int width, int height, bool waitUntilDone = false)
    {
        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
        {
            Texture = new Texture2D(width, height, TextureFormat.BGRA32, false);
        }, waitUntilDone);
    }

    public void SetBytes(byte[] image, bool waitUntilDone = true)
    {
        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
        {
            try
            {
                Texture.LoadRawTextureData(image);
                Texture.Apply();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }, waitUntilDone);
    }
}