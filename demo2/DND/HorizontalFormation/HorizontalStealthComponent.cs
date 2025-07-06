using UnityEngine;
using DND5E;

/// <summary>
/// 潜行状态枚举
/// </summary>
public enum StealthState {
    Visible,    // 可见状态
    Hidden,     // 隐身状态
    Flanking    // 背刺状态
}

/// <summary>
/// 横版战斗潜行组件
/// 处理盗贼的隐身和背刺机制
/// </summary>
public class HorizontalStealthComponent : MonoBehaviour {
    [Header("潜行设置")]
    public StealthState stealthState = StealthState.Visible;
    public float stealthDuration = 10f; // 潜行持续时间
    public int stealthBonus = 2; // 潜行检定加值
    public bool canFlankThisTurn = false; // 本回合是否可以背刺

    private CharacterStats character;
    private float stealthTimer = 0f;

    void Start() {
        character = GetComponent<CharacterStats>();
        if (character == null) {
            Debug.LogError("HorizontalStealthComponent需要CharacterStats组件");
        }
    }

    void Update() {
        if (stealthState == StealthState.Hidden || stealthState == StealthState.Flanking) {
            stealthTimer -= Time.deltaTime;
            if (stealthTimer <= 0f) {
                ExitStealth();
            }
        }
    }

    /// <summary>
    /// 尝试进入潜行状态
    /// </summary>
    public bool AttemptStealth() {
        if (character == null || character.characterClass != CharacterClass.Rogue) {
            return false;
        }

        if (stealthState != StealthState.Visible) {
            return false; // 已经在潜行状态
        }

        // 简化的潜行检定：基于敏捷值
        int stealthRoll = Random.Range(1, 21); // 1d20
        int stealthCheck = stealthRoll + GetDexterityModifier() + stealthBonus;

        if (stealthCheck >= 15) // DC 15
        {
            EnterStealth();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 尝试背刺移动
    /// </summary>
    public bool AttemptFlankingManeuver() {
        if (stealthState != StealthState.Hidden) {
            return false;
        }
        stealthState = StealthState.Flanking;
        stealthTimer = 3f; // 背刺状态持续3秒
        canFlankThisTurn = true; // 允许本回合背刺
        Debug.Log($"{character.characterName} 进入背刺状态");
        return true;
    }

    /// <summary>
    /// 进入潜行状态
    /// </summary>
    private void EnterStealth() {
        stealthState = StealthState.Hidden;
        stealthTimer = stealthDuration;
        Debug.Log($"{character.characterName} 进入潜行状态");
    }

    /// <summary>
    /// 退出潜行状态
    /// </summary>
    public void ExitStealth() {
        stealthState = StealthState.Visible;
        stealthTimer = 0f;
        Debug.Log($"{character.characterName} 退出潜行状态");
    }

    /// <summary>
    /// 获取敏捷调整值
    /// </summary>
    private int GetDexterityModifier() {
        // 简化版本：假设敏捷值为14，调整值为+2
        return 2;
    }

    /// <summary>
    /// 检查是否在潜行状态
    /// </summary>
    public bool IsStealthed() {
        return stealthState == StealthState.Hidden || stealthState == StealthState.Flanking;
    }    /// <summary>
         /// 检查是否在背刺状态
         /// </summary>
    public bool IsFlanking() {
        return stealthState == StealthState.Flanking;
    }

    /// <summary>
    /// 回合结束处理
    /// </summary>
    public void OnTurnEnd() {
        // 重置背刺标记
        canFlankThisTurn = false;

        // 如果在背刺状态且时间到了，退出潜行
        if (stealthState == StealthState.Flanking && stealthTimer <= 0f) {
            ExitStealth();
        }
    }
}
