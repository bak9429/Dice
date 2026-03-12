using UnityEngine;

namespace UI.UIControl
{
    // ✅ 분리된 partial 파일들의 "루트" 클래스 선언.
    // Unity는 이 파일(클래스)을 통해 컴포넌트를 붙인다.
    public partial class CombatUIController : MonoBehaviour
    {
        // 일부러 비워둠.
        // 실제 로직/필드는 CombatUIController.Core.cs / .Attack.cs / .Panel.cs 등 partial들에 있음.
    }
}