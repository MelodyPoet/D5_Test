using DG.Tweening;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
 
    Role role;
    void Start()
    {
        role = GetComponent<Role>();
    }
 
    void Update()
    {
        if (role.isDead)
        {
            return;
        }
        Vector2Int dir=Vector2Int.zero;
 
        if (Time.time - role.lastAttackTime < 1) return;

        var hero=GameObject.FindFirstObjectByType<HeroController>();
        if (Mathf.Abs(transform.position.x - hero.transform.position.x) > 8) {
            role.playAct(Role.ActState.IDLE);
            return; }
        var targetRelativePos = hero.transform.position - transform.position;
        if (Mathf.Abs(targetRelativePos.x) > 0.5)
            dir.x = (int)Mathf.Sign(targetRelativePos.x);
        if(Mathf.Abs( targetRelativePos.y)>0.1)
        dir.y = (int)Mathf.Sign(targetRelativePos.y);
        if (Mathf.Abs(transform.position.x- hero.transform.position.x)<3&& Mathf.Abs(transform.position.y - hero.transform.position.y) < 0.3)
        {
            if ( (Time.time - role.lastAttackTime > 2.2f))
            {
                role.playAct( Role.ActState.ATTACK, "sword_attack");
                role.lastAttackTime = Time.time;
                transform.Find("hit").gameObject.SetActive(false);
                transform.Find("hit").gameObject.SetActive(true);
                return;
            }
            role.playAct(Role.ActState.IDLE);
            return;
        }
    
 
        if(role.lastFaceDir != dir.x&& dir.x!=0) {
            transform.rotation = Quaternion.Euler(0, (-dir.x)*90+90,0);
           role.lastFaceDir = dir.x;
        }
        
        var pos = transform.position;
        pos.x+=dir.x*Time.deltaTime* role.speed;
        pos.y+=dir.y*Time.deltaTime * role.speed/2;
        if (dir != Vector2Int.zero) {
            role.playAct( Role.ActState.MOVE, "run_shield");
           
        }
        else {
            role.playAct(Role.ActState.IDLE);

        }
        transform.position = pos;
 
    }
}
