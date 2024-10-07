// Hook MouseLook.UpdateRotation and make it this:
static bool Prefix()
{
    var finishBounds = UnityEngine
        .GameObject.Find("Actors/FinalCutscene/FinaleDarknessPlane (2)")
        .GetComponent<UnityEngine.BoxCollider>()
        .bounds;
    var finishTopMiddle =
        finishBounds.m_Center + new UnityEngine.Vector3(1f, finishBounds.m_Extents.y - 1f, 0f);
    var player = UnityEngine.GameObject.Find("Player").GetComponent<FirstPersonDrifter>();
    var cameraPos = player.mouseLookY.transform.position;
    var direction = finishTopMiddle - player.mouseLookY.transform.position;
    var lookRot = UnityEngine.Quaternion.LookRotation(direction, UnityEngine.Vector3.up);
    var lookRotEuler = lookRot.eulerAngles;

    player.m_cameraRotationX = UnityEngine.Quaternion.Euler(0f, lookRotEuler.y, 0f);
    player.m_cameraRotationY = UnityEngine.Quaternion.Euler(lookRotEuler.x, 0f, 0f);
    // lookRotEuler.ToString();

    return false;
}

// PlayerWinTrigger.OnTriggerEnter(Collider)
// static bool Prefix() { UnityExplorer.ExplorerCore.Log("======= WIN"); return false; }
