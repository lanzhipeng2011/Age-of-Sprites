﻿using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using System.Collections.Generic;

namespace NSprites
{
    public class SpriteRendererAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        static readonly Dictionary<Texture, Material> _overridedMaterials = new();

        [SerializeField] public bool ExcludeUnityTransformComponents = true;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private SpriteRenderData _spriteRenderData;
        [SerializeField] public float2 scale = new(1f);
        [SerializeField] private bool _overrideSpriteTexture;
        [SerializeField] private float2 _pivot = new(.5f);
        [Space]
        [SerializeField] private bool _disableSorting;
        [Tooltip("Use it when entities exists on the same layer and never changes theirs position / sorting index / layer")]
        [SerializeField] private bool _staticSorting;
        [SerializeField] private int _sortingIndex;
        [SerializeField] private int _sortingLayer;

        public float2 VisualSize => new float2(_sprite.bounds.size.x, _sprite.bounds.size.y) * scale;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddSpriteRenderComponents(entity);

            if (!_disableSorting)
            {
                _ = dstManager.AddComponentData(entity, new VisualSortingTag());
                _ = dstManager.AddComponentData(entity, new SortingIndex { value = _sortingIndex });
                _ = dstManager.AddSharedComponentData(entity, new SortingLayer { index = _sortingLayer });

                if(_staticSorting)
                    _ = dstManager.AddComponentData(entity, new SortingStaticTag());
            }
            _ = dstManager.AddComponentData(entity, new SortingValue());

            _ = dstManager.AddComponentData(entity, new Pivot { value = _pivot });
            _ = dstManager.AddComponentData(entity, new Scale2D { value = VisualSize });
            _ = dstManager.AddComponentData(entity, new MainTexST { value = NSpritesUtils.GetTextureST(_sprite) });
            _ = dstManager.AddComponentData(entity, new MainTexSTInitial { value = NSpritesUtils.GetTextureST(_sprite) });
            var data = _spriteRenderData;
            if (_overrideSpriteTexture)
                data.Material = GetOrCreateOverridedMaterial(_sprite.texture);
            dstManager.AddComponentObject(entity, new SpriteRenderDataToRegistrate { data = data });
        }
        private Material GetOrCreateOverridedMaterial(Texture texture)
        {
            if (!_overridedMaterials.TryGetValue(texture, out var material))
                material = CreateOverridedMaterial(texture);
#if UNITY_EDITOR //for SubScene + domain reload
            else if (material == null)
            {
                _ = _overridedMaterials.Remove(texture);
                material = CreateOverridedMaterial(texture);
            }
#endif
            return material;
        }
        private Material CreateOverridedMaterial(Texture texture)
        {
            var material = new Material(_spriteRenderData.Material);
            material.SetTexture("_MainTex", _sprite.texture);
            _overridedMaterials.Add(texture, material);
            return material;
        }
    }
}
