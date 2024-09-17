using System.Collections;
using DesignPatterns;
using DG.Tweening;
using Fusion;
using Helpers;
using Networking;
using UnityEngine;
using UnityEngine.Pool;

public class CombatFightState : State<PlayerCombatBehavior>
{
    public CombatFightState(PlayerCombatBehavior context, StateMachine<PlayerCombatBehavior> stateMachine) : base(context, stateMachine)
    {
    }
    
    private float _lastShotTime = 0f;
    private float _recoilAmount = 0.019f;
    private WeaponStatsSO _weaponStats;
    private Vector3 _shotDirection;
    private RaycastHit _hit;
    private Vector3 _targetPoint;
    bool _hitSomething = false;
    private ConstantsManager.TargetType _targetType;
    
    private IObjectPool<BulletTrailBehavior> _bulletTrailPool;
    
    private bool _initialized;

    private Sequence _gunRecoilSequence;

    private bool _isCooldownComplete;
    private bool _hasBullets;
    
    public override void Enter()
    {
        if (!_initialized)
        {
            InitPools();

            var originalPosZ = context.PlayerWeaponHolder.localPosition.z;
            
            _gunRecoilSequence = 
                DOTween.Sequence().Append(context.PlayerWeaponHolder.DOLocalMoveZ(originalPosZ - _recoilAmount, 0.05f).SetEase(Ease.OutSine))
                .Append(context.PlayerWeaponHolder.DOLocalMoveZ(originalPosZ, 0.1f).SetEase(Ease.InOutSine))
                .Pause()
                .SetAutoKill(false);
            
            _initialized = true;
        }
            
        Debug.Log("<color=magenta>Entered combat fight state</color>");
        _weaponStats = context.PistolStats;
    }

    private void InitPools()
    {
        _bulletTrailPool = new ObjectPool<BulletTrailBehavior>(CreateTrailPrefab, OnGetFromTrailPool,
            OnReleaseToTrailPool, OnDestroyTrailOnPool, false, 20, 100);
    }
    
    public override void LogicUpdate()
    {
        _isCooldownComplete = Time.time - _lastShotTime > _weaponStats.DelayBetweenShots;
        _hasBullets = context.BulletsLeft > 0;
        
        if(context.InputReader.ReloadStatus && context.BulletsLeft < context.PistolStats.BulletsPerClip)
            stateMachine.ChangeState(context.CombatReloadState);
        
        if (context.InputReader.ShootStatus)
        {
            if (_hasBullets && _isCooldownComplete)
                Shoot();
            else if (!_hasBullets && _isCooldownComplete) 
                stateMachine.ChangeState(context.CombatReloadState); //will only auto-reload when the shoot cooldown is complete
        }
        else
            stateMachine.ChangeState(context.CombatIdleState);
    }

    private void Shoot()
    {
        context.BulletsLeft--;
        Debug.Log($"Bullets Left{context.BulletsLeft}");
        
        _lastShotTime = Time.time;
        _shotDirection = GetDirection();
        _hitSomething = false;
        _targetType = ConstantsManager.TargetType.NONE;

        GunRecoilAnimation();
        context.MuzzleFlashParticle.Play();
        
        if (Physics.Raycast(context.PlayerCameraTransform.position, _shotDirection, out _hit, float.MaxValue, _weaponStats.Mask))
        {
            _targetPoint = _hit.point;
            _targetType = GetLayerHit(_hit.collider.gameObject.layer);
            _hitSomething = true;
        }
        else
            _targetPoint = context.PlayerCameraTransform.position + _shotDirection * 1000f; // If no hit, calculate a faraway point in the shooting direction
        
        _bulletTrailPool.Get();
        
        //sending trail stats to puppet
        context.LocalPlayerToPuppetSynchronizer.SetBulletTrailData(new BulletTrailNetworkData(_targetPoint, _hitSomething, _hit.normal, _shotDirection, _targetType));
    }

    private void GunRecoilAnimation()
    {
        _gunRecoilSequence.Restart();
    }

    private ConstantsManager.TargetType GetLayerHit(int layer)
    {
        string layerName = LayerMask.LayerToName(layer);
        
        return layerName switch
        {
            ConstantsManager.PLAYER_HIT_DETECTION_COLLIDER_LAYER or ConstantsManager.NETWORK_PLAYER_HIT_DETECTION_COLLIDER_LAYER => ConstantsManager.TargetType.HUMAN,
            ConstantsManager.ENEMY_LAYER => ConstantsManager.TargetType.MONSTER,
            _ => ConstantsManager.TargetType.METAL
        };
    }
    
    private Vector3 GetDirection()
    {
        Vector3 direction = context.PlayerCameraTransform.forward;

        if (_weaponStats.HasBulletSpread)
        {
            var spreadVariance = _weaponStats.BulletSpreadVariance;
            var spreadX = Random.Range(-spreadVariance.x, spreadVariance.x);
            var spreadY = Random.Range(-spreadVariance.y, spreadVariance.y);
            var spreadZ = Random.Range(-spreadVariance.z, spreadVariance.z);
            
            direction += new Vector3(spreadX,spreadY,spreadZ);
            
            direction.Normalize();
        }
        

        return direction;
    }

    private BulletTrailBehavior CreateTrailPrefab()
    {
        var newPoolObject = Object.Instantiate(context.BulletTrailPrefab, context.GunMuzzleRef.position, Quaternion.LookRotation(_shotDirection));
        newPoolObject.ObjectPool = _bulletTrailPool;
        return newPoolObject;
    }

    private void OnReleaseToTrailPool(BulletTrailBehavior pooledObject)
    {
        pooledObject.gameObject.SetActive(false);
    }
    
    private void OnGetFromTrailPool(BulletTrailBehavior pooledObject)
    {
        pooledObject.transform.SetPositionAndRotation(context.GunMuzzleRef.position, Quaternion.LookRotation(_shotDirection));
        pooledObject.transform.position += context.MuzzleWorldVelocity * Time.deltaTime; //moving the trail to fit neatly on the muzzle position, without this, moving and shooting the bullet starts to instantiate on the air instead of the muzzle pos 
        pooledObject.Initialize(_targetPoint, _hitSomething, _targetType , _hit.normal);
        
        pooledObject.gameObject.SetActive(true);
    }
    
    private void OnDestroyTrailOnPool(BulletTrailBehavior pooledObject)
    {
        Object.Destroy(pooledObject.gameObject);
    }
    
    
    public override void PhysicsUpdate()
    {

    }

    public override void Exit()
    {

    }
}
