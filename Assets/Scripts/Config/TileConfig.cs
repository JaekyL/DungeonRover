using System.Collections.Generic;
using Helper;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Config
{
    [CreateAssetMenu(menuName = "Game/TileConfig Asset", fileName = "TileConfig")]
    [InlineEditor()]
    public class TileConfig : SerializedScriptableObject
    {
        public static Dictionary<string, BlobAssetReference<TileStats>> Allocations =
            new Dictionary<string, BlobAssetReference<TileStats>>();
        
        [BoxGroup("Type")]
        [SerializeField] private TileType tileType;

        [HorizontalGroup("Visuals", 75)]
        [PreviewField(75)] 
        [HideLabel]
        [SerializeField] private Material tileBaseMaterial;
        
        [VerticalGroup("Visuals/Specifics")]
        [LabelWidth(100)]
        [SerializeField] private Color color;

        [BoxGroup("Stats")]
        [LabelWidth(100)]
        [SerializeField] 
        private int randomWeight;
        
        [BoxGroup("Stats")]
        [LabelWidth(100)]
        [SerializeField] 
        private int health;
        
        [BoxGroup("Stats")]
        [LabelWidth(100)]
        [SerializeField] 
        private int hardness;

        public BlobAssetReference<TileStats> ToBlobAssetReference()
        {
            if (Allocations.ContainsKey(this.name))
            {
                return Allocations[this.name];
            }
            else
            {
                BlobAssetReference<TileStats> tileStats;

                {
                    var builder = new BlobBuilder(Allocator.Temp);
                    ref var root = ref builder.ConstructRoot<TileStats>();

                    {
                        root.Type = tileType;
                    }
                    
                    {
                        root.Color = color;
                    }

                    {
                        root.Health = health;
                    }

                    {
                        root.Hardness = hardness;
                    }

                    {
                        root.Weight = randomWeight;
                    }
                    
                    tileStats = builder.CreateBlobAssetReference<TileStats>(Allocator.Persistent);
                    builder.Dispose();
                }
                
                Allocations.Add(this.name, tileStats);

                return tileStats;
            }
        }

    }

    public struct TileStats
    {
        public TileType Type;
        public Color Color;
        public int Health;
        public int Hardness;
        public int Weight;
    }
}