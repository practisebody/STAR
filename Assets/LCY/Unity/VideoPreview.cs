using UnityEngine;
using UnityEngine.UI;

namespace LCY
{
    /// <summary>
    /// Unity VideoPreview object, that renders a RawImage using given texture data
    /// </summary>
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
            Utilities.InvokeMain(() =>
            {
                Texture = new Texture2D(width, height, TextureFormat.BGRA32, false);
            }, waitUntilDone);
        }

        public void SetBytes(byte[] image, bool waitUntilDone = true)
        {
            Utilities.InvokeMain(() =>
            {
                Texture.LoadRawTextureData(image);
                Texture.Apply();
            }, waitUntilDone);
        }
    }
}