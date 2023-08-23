using UnityEditor;
using UnityEngine;

namespace Flippit.Editor
{
    public static class ThumbnailGenerator
    {
        private const string ThumbnailFolder = "Assets/Flippit/Resources/Thumbnails";
        private static string characName;

        public static Texture2D GenerateThumbnail(GameObject gameObject, string characterName,int width, int height)
        {
            Camera camera = EditorUtility.CreateGameObjectWithHideFlags("ThumbnailCamera", HideFlags.HideAndDontSave, typeof(Camera)).GetComponent<Camera>();
            RenderTexture renderTexture = new(width, height, 24);
            camera.targetTexture = renderTexture;
            characName = characterName;
            // Positionner et orienter la caméra pour le thumbnail
            float distance = -1f; // Distance de la caméra au GameObject
            Bounds objectBounds = gameObject.GetComponent<Collider>().bounds;
            Vector3 targetPosition = new(0, objectBounds.size.y / 2f, 0);
            Vector3 cameraOffset = new(1, 1, 0);
            Vector3 cameraPosition = targetPosition - camera.transform.forward * distance+cameraOffset;
            Quaternion cameraRotation = Quaternion.LookRotation(targetPosition - cameraPosition);

            camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);

            // Isoler le gameObject spécifié
            SkinnedMeshRenderer[] skinnedRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            
            
            if (skinnedRenderers.Length == 0)
            {
                MeshRenderer[] meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();
                foreach(MeshRenderer render in meshRenderers)
                {
                    render.gameObject.layer = 1;
                }
            }
            else
            {
                foreach (SkinnedMeshRenderer renderer in skinnedRenderers)
                {
                    renderer.gameObject.layer = 1;
                }
            }


            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.white;
            camera.cullingMask = 1 << 1;

            //Posing
            Animator animator = gameObject.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                // Mettez l'animation en pause à une position spécifique
                animator.Play("Locomotion", 0, 0f); // Remplacez "YourAnimationStateName" par le nom de l'état d'animation souhaité

                // Vous pouvez également régler d'autres paramètres de l'Animator selon vos besoins
                animator.speed = 0f; // Pour mettre l'animation en pause complète
                //animator.enabled = false; // Pour désactiver complètement l'Animator
            }


            camera.Render();

            RenderTexture.active = renderTexture;

            Texture2D texture = new(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();

            RenderTexture.active = null;
            camera.targetTexture = null;

            // Détruire la caméra et le rendertexture après avoir récupéré le thumbnail
            Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(renderTexture);
            foreach (SkinnedMeshRenderer renderer in skinnedRenderers)
            {
                renderer.gameObject.layer = 0;
            }

            if (!AssetDatabase.IsValidFolder(ThumbnailFolder))
            {
                AssetDatabase.CreateFolder("Assets/Flippit/Resources", "Thumbnails");
            }
            SaveThumbnail(texture);
            
            return texture;
        }

        private static void SaveThumbnail(Texture2D texture)
        {
            string fileName = characName+".png";
            string filePath = System.IO.Path.Combine(ThumbnailFolder, fileName);
            byte[] bytes = texture.EncodeToPNG();

            System.IO.Directory.CreateDirectory(ThumbnailFolder);
            System.IO.File.WriteAllBytes(filePath, bytes);

            AssetDatabase.ImportAsset(filePath);
            TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(filePath);
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            textureImporter.alphaIsTransparency = true;
            textureImporter.mipmapEnabled = false;
            textureImporter.SaveAndReimport();
        }
    }
}
