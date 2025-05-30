using DG.Tweening;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using UnityEngine;
using Random = UnityEngine.Random;

public class Role : MonoBehaviour
{
    public enum ActState
    {
        IDLE,MOVE,ATTACK,JUMP,HIT,DEAD
    }
    public float speed = 1;
    private SkeletonAnimation skeletonAnimation;
    public int lastFaceDir;
    public float lastAttackTime = 0;
    public GameObject hpBar;
    private Vector3 hpBarRPos;
    public int hp;
    public int hpMax;
    public ActState actState = ActState.IDLE;
    public bool isDead { get {return hp <= 0; }  }

    void OnHit(int dir)
    {
        if (isDead)
        { 
            return;
        }
            lastAttackTime = Time.time - 0.13f;
        skeletonAnimation.AnimationName = "hit";
        var _tweener = transform.DOBlendableLocalMoveBy(new Vector3(dir, 0, 0), 0.3f);
        _tweener.SetAutoKill(true);
        _tweener.PlayForward();
        hp -= Random.Range(10, 20);
        hp=Mathf.Max(0, hp);
        if (isDead) {
            playAct(ActState.DEAD);
        }
        hpBar.GetComponent<SpriteRenderer>().size =new Vector2( 1.33f* hp /hpMax,0.2f);
    }
    void Start()
    {
        hp = hpMax = 100;
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        hpBarRPos = hpBar.transform.position - transform.position;

    }

    // Update is called once per frame
    void Update()
    {
        GetComponent<MeshRenderer>().sortingOrder = 100 - (int)((transform.position.y - LevelData.WalkRect.yMin) / LevelData.WalkRect.height * 90);
        hpBar.transform.position = hpBarRPos + transform.position;
    }

    internal void playAct(ActState actState,string anmName=null)
    {
       this.actState = actState;
        switch (actState)
        {
            case ActState.IDLE:
                 playAnimation("idle_3", true);
                break;
            case ActState.MOVE:
                playAnimation(anmName, true);
                break;
            case ActState.ATTACK:
                playAnimation(anmName, false);
                break;
            case ActState.JUMP:
                playAnimation("jump", false);
                break;
            case ActState.HIT:
                playAnimation("hit", false);
                break;

            case ActState.DEAD:
                playAnimation("dead", false);
              
              
              
                break;
            default:
                break;
        }

    }
    internal void playAnimation(string anm,bool loop=false)
    {
        skeletonAnimation.timeScale = 2;
        skeletonAnimation.loop = loop;
        skeletonAnimation.AnimationName = anm;
       
    }
}
