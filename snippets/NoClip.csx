GameObject
    .Find("Level Art/LargeColiseumCullingTrigger/port_coliseum/coliseum_LOD_concrete")
    .SetActive(false);
var drifter = GameObject.Find("Player").GetComponent<FirstPersonDrifter>();
drifter._noclip = true;
drifter.currentMode.moveSpeed = drifter.currentMode.moveSpeedMax = 600f;
