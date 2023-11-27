using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Fox.Maps.AssetManagement
{
    public class AssetDB
    {
        private List<Asset> assets = new List<Asset>();

        public AssetDB(AssetBundle bundle)
        {
            this.Append(bundle);
        }

        public UnityEngine.Object this[string assetName]
        {
            get
            {
                return GetByName(assetName);
            }
        }

        public void Append(AssetBundle bundle)
        {
            if (!bundle)
                throw new ArgumentNullException("bundle");

            string[] paths = bundle.GetAllAssetNames();
            foreach(string path in paths)
            {
                Asset asset = Asset.Create(path, bundle.LoadAsset(path));

                assets.Add(asset);
            }
        }

        public UnityEngine.Object GetByName(string name)
        {
            foreach(Asset asset in assets)
                if (asset.assetName == name)
                    return asset.asset;
            return null;
        }

        public T GetByName<T>(string name) where T : UnityEngine.Object
        {
            foreach (Asset asset in assets) 
            {
                if (asset.assetName == name.ToLower()) 
                {
                    return asset.asset as T;
                }
            }
            return null;
        }

        public UnityEngine.Object GetByPath(string path)
        {
            foreach (Asset asset in assets)
                if (asset.assetPath == path)
                    return asset.asset;
            return null;
        }

        public T GetByPath<T>(string path) where T : UnityEngine.Object
        {
            foreach (Asset asset in assets)
                if (asset.assetPath == path)
                    return asset.asset as T;
            return null;
        }

        public UnityEngine.Object GetByFileName(string fileName)
        {
            foreach (Asset asset in assets)
                if (asset.assetFileName == fileName)
                    return asset.asset;
            return null;
        }


        private struct Asset
        {
            public UnityEngine.Object asset;

            public string assetPath;
            public string assetName;
            public string assetFileName;

            public static Asset Create(string path, UnityEngine.Object asset)
            {
                Asset assetInstance = new Asset
                {
                    asset = asset,
                    assetPath = path
                };
                try
                {

                    string[] pathParts = path.Split('/');
                    string assetName = pathParts[pathParts.Length - 1];
                    assetInstance.assetFileName = assetName;
                    assetInstance.assetName = assetName.Split('.')[0];
                }
                catch(Exception ex)
                {
                    Mod.Log(ex);
                }

                return assetInstance;
            }
        }
    }


    public class AssetBundleManager
    {
        public static AssetDB Unpack(string path, string bundleName)
        {
            AssetBundle bundle = KCModHelper.LoadAssetBundle(path, bundleName);

            return new AssetDB(bundle);
        }
    }
}
