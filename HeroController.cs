using DG.Tweening;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeroController : MonoBehaviour
{
    Role role;
    int test = 0;
    void Start()
    {
        role=GetComponent<Role>();
        test++;
        Debug.Log(test);
        test++;
        Debug.Log(test);
    }
    Vector2Int lockDir;
    // Update is called once per frame
    void Update()
    {
        if (role.isDead)
        {
            return;
        }
        if (role.actState== Role.ActState.JUMP)
        {
            moveBy(lockDir);
            if (Time.time - role.lastAttackTime >0.7f) {
                role.playAct(Role.ActState.IDLE);
            }
            return;
        }
        Vector2Int dir=Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.J)&&(Time.time - role.lastAttackTime > 1.2f)) 
        {
            role.playAct( Role.ActState.ATTACK, "sword_attack");
            transform.Find("hit").gameObject.SetActive(false);
            transform.Find("hit").gameObject.SetActive(true);
            role.lastAttackTime = Time.time;
        }
        if (Input.GetKeyDown(KeyCode.K) && (Time.time - role.lastAttackTime > 1.2f))
         {
            role.playAct(Role.ActState.ATTACK, "shield_attack");
            transform.Find("hit").gameObject.SetActive(false);
            transform.Find("hit").gameObject.SetActive(true);
            role.lastAttackTime = Time.time;
        }
 
        if (Time.time - role.lastAttackTime < 1) return;
        if (Input.GetKey(KeyCode.A)) { dir.x = -1; }
        if(Input.GetKey(KeyCode.D)) { dir.x = 1; }

        if (Input.GetKey(KeyCode.W)) { dir.y = 1; }
        if (Input.GetKey(KeyCode.S)) { dir.y = -1; }
        if (role.lastFaceDir !=dir.x&& dir.x!=0) {
            transform.rotation = Quaternion.Euler(0, (-dir.x)*90+90,0);
            role.lastFaceDir = dir.x;
        }
        if (Input.GetKeyDown(KeyCode.Space) && (Time.time - role.lastAttackTime > 1.2f))
        {
            role.playAct  ( Role.ActState.JUMP);
            lockDir = dir;
            
            role.lastAttackTime = Time.time;
        }
        moveBy(dir);
    }

    private void moveBy(Vector2Int dir)
    {
        var pos = transform.position;
        pos.x += dir.x * Time.deltaTime * role.speed;
        pos.y += dir.y * Time.deltaTime * role.speed / 2;
        pos.x = Mathf.Clamp(pos.x, LevelData.WalkRect.xMin, LevelData.WalkRect.xMax);
        pos.y = Mathf.Clamp(pos.y, LevelData.WalkRect.yMin, LevelData.WalkRect.yMax);
        if (dir != Vector2Int.zero)
        {
            if(role.actState != Role.ActState.JUMP)
            role.playAct(Role.ActState.MOVE, "run_shield");

        }
        else if(role.actState == Role.ActState.MOVE) 
        {
            role.playAct( Role.ActState.IDLE);

        }
        transform.position = pos;
        var c_pos = Camera.main.transform.position;
        c_pos.x = Mathf.Lerp(c_pos.x, pos.x, 0.1f);

        Camera.main.transform.position = c_pos;
    }
}
