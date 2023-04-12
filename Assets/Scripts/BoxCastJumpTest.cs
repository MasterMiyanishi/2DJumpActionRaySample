// ---------------------------------------------------------  
// BoxCastJumpTest.cs  
//   BoxCastを使ったジャンプサンプル（2D）
// 作成日:  2022/11/21
// 作成者:  MasterM
// ---------------------------------------------------------  
using UnityEngine;
using System.Collections;

public class BoxCastJumpTest : MonoBehaviour
{

    #region 変数  
    bool _isJump = false;
    float _jumpPower = 2f;
    #endregion

    #region プロパティ  

    #endregion

    #region メソッド  
    private void Update() {

        // BoxCastにコライダーが当たったかどうかの状況がRaycastHitに入ります
        RaycastHit2D hit;
        /* Physics2D.BoxCastでBoxCastを飛ばします
         * Raycast(飛ばす位置,箱の大きさ,回転角,飛ばす向き,飛ばす長さ)
         */

        // BoxCastの大きさ用
        float boxSizeX = transform.localScale.x;
        float boxSizeY = 0.05f;

        // BoxCastの開始位置
        Vector2 startPosition = transform.position - transform.up * (0.51f +boxSizeY/2);
        // 発射する箱の大きさ
        Vector2 boxSize = new Vector2(boxSizeX, boxSizeY);
        // 飛ばす向き
        Vector2 rotation = -transform.up;
        // 回転角
        float angle = 0f;
        // 飛ばす長さ
        float rayDistance = 0.1f;

        hit = Physics2D.BoxCast(startPosition, boxSize, angle, rotation, rayDistance);


        // 描画の時間
        float drawDuration = 0.1f;
        //DrawRayは見えないRaycastを描画します（設定する値はRayCastと同じ値を入れてください）
        Debug.DrawRay(startPosition, rotation * rayDistance, Color.red, drawDuration);

        // hitにデータが合ったら
        if (hit) {
            Debug.Log(hit.transform.name);

            // ジャンプボタンが押されたら
            if (Input.GetButtonDown("Jump")) {
                _isJump = true;
            }
        }

    }
    private void FixedUpdate() {
        if (_isJump) {
            GetComponent<Rigidbody2D>().velocity = new Vector2(
                GetComponent<Rigidbody2D>().velocity.x,
                _jumpPower
                );
            _isJump = false;
        }
    }
    #endregion
}