using UnityEngine;

[System.Serializable]
public class BattleFieldSetup
{
    // 战场距离配置
    public const float BASE_MOVEMENT_RANGE = 30f;     // DND基础移动距离和远程攻击距离（30尺）
    public const float UNIT_SCALE = 1f;              // Unity单位与DND尺度的换算比例
    public const float ROW_SPACING_RATIO = 0.33f;    // 排距比例为基准距离的1/3，确保三排阵型刚好占满一个基准距离
    public const float SIDE_SPACING_RATIO = 0.25f;    // 横向间距为排距的75%左右，确保阵型紧凑但不拥挤

    // 布局参数，都基于BASE_MOVEMENT_RANGE计算
    public float rowSpacing = BASE_MOVEMENT_RANGE * 0.2f;      // 同阵营内部排距是移动范围的20%
    public float sideSpacing = BASE_MOVEMENT_RANGE * 0.15f;    // 横向间距是移动范围的15%
    
    // 自动计算的布局参数
    public Vector3 fieldCenter;            // 战场中心点
    public float battleFieldLength;        // 战场总长度（基于BASE_MOVEMENT_RANGE）    
    
    public void InitializeLayout(Vector3 center)
    {
        fieldCenter = center;
        
        // 战场总长度 = 基础移动距离 * 2（确保双方远程单位恰好能互相接触到前排）
        battleFieldLength = BASE_MOVEMENT_RANGE * 2;
        
        // 每个阵营占据30尺空间，分成三等份作为排距（每份10尺）
        rowSpacing = BASE_MOVEMENT_RANGE / 3;    // 10尺，确保三排阵型正好占满30尺空间
        sideSpacing = rowSpacing * 0.75f;       // 7.5尺，横向间距略小于纵向间距
    }    
    
    // 计算实际生成点的位置
    public Vector3 GetSpawnPosition(int row, int col, bool isPlayerSide)
    {
        float halfLength = battleFieldLength * 0.5f;
        
        // Z轴位置（前后）- 基于sideSpacing计算横向偏移
        float zPos = row * sideSpacing - sideSpacing;  // 居中对齐
        
        // X轴位置（左右）计算
        float xPos;
        if (isPlayerSide)
        {
            // 玩家方在左侧
            xPos = -halfLength + (col * rowSpacing);  // col: 0=后排, 1=中排, 2=前排
        }
        else
        {
            // 敌人方在右侧，且从前排开始
            xPos = halfLength - ((2-col) * rowSpacing);  // 2-col: 2=前排, 1=中排, 0=后排
        }
        
        return new Vector3(xPos * UNIT_SCALE, 0, zPos * UNIT_SCALE) + fieldCenter;
    }

    // 验证生成位置是否有效
    public bool ValidateSpawnPosition(Vector3 position)
    {
        // 使用射线检测确保位置在地面上
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 10, Vector3.down, out hit, 20f))
        {
            return hit.collider != null && !hit.collider.isTrigger;
        }
        return false;
    }
}
