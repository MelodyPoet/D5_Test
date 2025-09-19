using UnityEngine;
using demo2.DND;
using demo2.DND.HorizontalFormation;

namespace demo2.DND
{
    /// <summary>
    /// 动画系统测试器 - 用于测试DOTween+SpineEvent动画系统
    /// </summary>
    public class AnimationSystemTester : MonoBehaviour
    {
        [Header("测试目标")]
        public DND_CharacterAdapter testCharacter;
        public Transform dummyTarget;

        [Header("测试按钮")]
        [Space(10)]
        public KeyCode testAttackKey = KeyCode.Alpha1;
        public KeyCode testHitKey = KeyCode.Alpha2;
        public KeyCode testDeathKey = KeyCode.Alpha3;
        public KeyCode testUnconsciousKey = KeyCode.Alpha4;
        public KeyCode testMeleeAttackKey = KeyCode.Alpha5;
        public KeyCode testRangedAttackKey = KeyCode.Alpha6;

        void Start()
        {
            // 自动查找测试目标
            if (testCharacter == null)
            {
                testCharacter = FindObjectOfType<DND_CharacterAdapter>();
            }

            // 创建虚拟目标
            if (dummyTarget == null)
            {
                GameObject dummy = new GameObject("DummyTarget");
                dummy.transform.position = transform.position + Vector3.right * 3f;
                dummyTarget = dummy.transform;
            }

            Debug.Log("动画系统测试器已就绪！");
            Debug.Log("测试按键：");
            Debug.Log($"[{testAttackKey}] - 测试攻击动画");
            Debug.Log($"[{testHitKey}] - 测试受击动画");
            Debug.Log($"[{testDeathKey}] - 测试死亡动画");
            Debug.Log($"[{testUnconsciousKey}] - 测试昏迷动画");
            Debug.Log($"[{testMeleeAttackKey}] - 测试近战攻击（位移+攻击+返回）");
            Debug.Log($"[{testRangedAttackKey}] - 测试远程攻击（原地攻击）");
        }

        void Update()
        {
            if (testCharacter == null) return;

            // 测试基础动画
            if (Input.GetKeyDown(testAttackKey))
            {
                TestAttackAnimation();
            }

            if (Input.GetKeyDown(testHitKey))
            {
                TestHitAnimation();
            }

            if (Input.GetKeyDown(testDeathKey))
            {
                TestDeathAnimation();
            }

            if (Input.GetKeyDown(testUnconsciousKey))
            {
                TestUnconsciousAnimation();
            }

            // 测试复合动画
            if (Input.GetKeyDown(testMeleeAttackKey))
            {
                TestMeleeAttack();
            }

            if (Input.GetKeyDown(testRangedAttackKey))
            {
                TestRangedAttack();
            }
        }

        /// <summary>
        /// 测试攻击动画
        /// </summary>
        private void TestAttackAnimation()
        {
            Debug.Log("测试攻击动画...");
            testCharacter.PlayAttackAnimation();
        }

        /// <summary>
        /// 测试受击动画
        /// </summary>
        private void TestHitAnimation()
        {
            Debug.Log("测试受击动画...");
            testCharacter.PlayHitAnimation();
        }

        /// <summary>
        /// 测试死亡动画
        /// </summary>
        private void TestDeathAnimation()
        {
            Debug.Log("测试死亡动画...");
            testCharacter.PlayDeathAnimation();
        }

        /// <summary>
        /// 测试昏迷动画
        /// </summary>
        private void TestUnconsciousAnimation()
        {
            Debug.Log("测试昏迷动画...");
            testCharacter.PlayUnconsciousAnimation();
        }

        /// <summary>
        /// 测试近战攻击（位移+攻击+返回）
        /// </summary>
        private void TestMeleeAttack()
        {
            Debug.Log("测试近战攻击（位移+攻击+返回）...");
            testCharacter.ExecuteMeleeAttack(
                dummyTarget,
                onAttackHit: () => {
                    Debug.Log("✅ SpineEvent触发：攻击命中！");
                },
                onComplete: () => {
                    Debug.Log("✅ 近战攻击序列完成，已返回原位！");
                }
            );
        }

        /// <summary>
        /// 测试远程攻击（原地攻击）
        /// </summary>
        private void TestRangedAttack()
        {
            Debug.Log("测试远程攻击（原地攻击）...");
            testCharacter.ExecuteRangedAttack(
                dummyTarget,
                onAttackHit: () => {
                    Debug.Log("✅ SpineEvent触发：远程攻击命中！");
                },
                onComplete: () => {
                    Debug.Log("✅ 远程攻击完成！");
                }
            );
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("动画系统测试面板", GUI.skin.box);

            if (testCharacter == null)
            {
                GUILayout.Label("未找到测试角色", GUI.skin.label);
                return;
            }

            GUILayout.Label($"当前动画: {testCharacter.CurrentAnimationName}");
            GUILayout.Label($"是否正在动画: {testCharacter.IsAnimating}");

            GUILayout.Space(10);

            if (GUILayout.Button("攻击动画"))
                TestAttackAnimation();

            if (GUILayout.Button("受击动画"))
                TestHitAnimation();

            if (GUILayout.Button("死亡动画"))
                TestDeathAnimation();

            if (GUILayout.Button("昏迷动画"))
                TestUnconsciousAnimation();

            GUILayout.Space(10);

            if (GUILayout.Button("近战攻击测试"))
                TestMeleeAttack();

            if (GUILayout.Button("远程攻击测试"))
                TestRangedAttack();

            GUILayout.EndArea();
        }
    }
}
