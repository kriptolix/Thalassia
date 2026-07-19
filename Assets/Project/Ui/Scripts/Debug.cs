using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Text;

public class DebugHUD : MonoBehaviour
{
    public TextMeshProUGUI text;

    public SailSystem sailSystem;
    public RudderSystem rudderSystem;
    public ShipMovementSystem movementSystem;    

    [Tooltip("Opcional - deixe vazio se ainda não estiver funcionando. O HUD usa ApparentWind do ShipMovementSystem como alternativa enquanto isso.")]
    public WindSystem windSystem;

    [Header("Log Automático")]
    [Tooltip("Se ativado, escreve um log formatado no Console a cada 'logInterval' segundos - útil para copiar e colar aqui sem transcrever a HUD na mão.")]
    public bool autoLogEnabled = true;
    public float logInterval = 1f;
    [Tooltip("Também loga imediatamente ao pressionar esta tecla.")]
    public Key manualLogKey = Key.L;

    private float _logTimer;

    void Update()
    {
        float shipHeading = movementSystem.CurrentShipHeadingDeg;

        // Vento aparente vem do ShipMovementSystem, que já calcula isso
        // corretamente (windVector - velocidade do barco), independente do
        // windSystem estar funcionando ou não.
        //
        // BUG CORRIGIDO: esta seção (windSection) era montada mas nunca
        // entrava em "text.text" (só o vento aparente aparecia na tela).
        // Como o vento aparente muda com a velocidade/guinada do barco
        // mesmo com o vento REAL travado via TestOrchestrator, parecia que
        // o override não estava funcionando quando na verdade só não
        // estava sendo exibido - o Console (LogSnapshot) sempre mostrou o
        // valor correto, só a HUD na tela que "escondia" essa seção.
        string windSection = "Vento (Real, via WindSystem):\n";
        if (windSystem != null)
        {
            ComputeShipRelativeWind(windSystem.CurrentDirection, shipHeading, out float trueAngleFromBow, out int trueSide);
            string trueArrow = GetRelativeWindArrow(trueAngleFromBow, trueSide);
            windSection += $"  {trueArrow} {trueAngleFromBow:F0}° da proa (Intensidade: {windSystem.CurrentIntensity:F1} m/s)\n\n";
        }
        else
        {
            windSection += "  (windSystem não atribuído / não funcionando)\n\n";
        }

        // Ângulo relativo à PROA em vez de rumo de bússola (0-360°) - em
        // alto mar, sem referência, "vento a 137°" não diz muito. A seta
        // mostra de onde o vento está vindo em relação ao barco (↑ = de
        // popa/empurrando, ↓ = de proa/contra - ver GetRelativeWindArrow).
        string apparentArrow = GetRelativeWindArrow(movementSystem.CurrentWindAngleFromBow, sailSystem.TrimSide);
        string apparentWindSection =
            $"Vento Aparente (via ShipMovementSystem):\n" +
            $"  {apparentArrow} {movementSystem.CurrentWindAngleFromBow:F0}° da proa | {movementSystem.ApparentWindSpeed:F1} m/s\n\n";

        string sailSection =
            $"Vela:\n" +
            $"  Abertura: {sailSystem.CurrentSailOpenAmount:P0} (alvo nível {sailSystem.TargetSailOpenLevel}/2:)\n" +
            //$"  Ângulo visual: {sailSystem.CurrentSailAngle:F1}°, lado {(sailSystem.TrimSide > 0 ? "+" : "-")}\n" +
            $"  Modo de navegação: {movementSystem.CurrentSailModeName} \n" +            
            //$"  Ângulo Vento-Proa: {movementSystem.CurrentWindAngleFromBow:F1}°\n" +
            $"  Faixa: {(movementSystem.CurrentInIdealRange ? "IDEAL" : movementSystem.CurrentInAcceptableRange ? "aceitável" : "FORA")}\n" +
            //$"  Eficiência do trim: {movementSystem.CurrentSailEfficiency:P0}\n" +
            //$"  Força bruta: {movementSystem.CurrentSailForce:F0} N\n" +
            $"  Empuxo (forward): {movementSystem.CurrentThrustForce:F0} N\n";// +
            //$"  Deriva (lateral): {movementSystem.CurrentLateralForce:F0} N\n\n";

        Vector3 rudderForce = rudderSystem.GetRudderForce();
        string rudderSection =
            $"Leme:\n" +
            $"  Ângulo: {rudderSystem.RudderAngle:F1}°\n" +
            //$"  Força (vetor): {rudderForce}\n" +
            $"  Força (magnitude): {rudderForce.magnitude:F1} N\n"; //+
            //$"  Turn Drag: {rudderSystem.GetTurnDragForceMagnitude():F1}\n\n";

        Vector3 localVel = movementSystem.transform.InverseTransformDirection(
            movementSystem.GetComponent<Rigidbody>().linearVelocity);

        string shipSection =
            $"Navio:\n" +
            $"  Heading: {movementSystem.CurrentShipHeadingDeg:F1}°\n" +
            //$"  Velocidade (m/s): {movementSystem.CurrentSpeed:F1}\n" +
            $"  Velocidade Lateral: ({localVel.x:F2})\n" +
            $"  Velocidade Frontal: ({localVel.z:F2})\n" ;//+
            //$"  Heel: {movementSystem.CurrentHeelAngle:F1}°\n" +
            //$"  Penalidade de arrasto (heel): x{movementSystem.CurrentHeelDragMultiplier:F2}\n" +
            //$"  Penalidade de manobra do leme (heel): {movementSystem.CurrentRudderHeelEfficiencyMultiplier:P0}";

        text.text = windSection + apparentWindSection + sailSection + rudderSection + shipSection;

        HandleLogging();
    }

    // Converte uma direção de vento em coordenadas do mundo (mesma
    // convenção do WindSystem: 0-360°, PARA ONDE o vento sopra) num ângulo
    // relativo à proa do barco (0=proa, 180=popa) + o lado de onde vem
    // (+1 estibordo, -1 bombordo) - mesma convenção de sinal usada pelo
    // "windSide" calculado internamente pelo ShipMovementSystem
    // (Vector3.Dot(apparentWind, transform.right)). Usado só pro vento REAL
    // (windSystem), já que o vento APARENTE já vem pronto nesse formato via
    // movementSystem.CurrentWindAngleFromBow + sailSystem.TrimSide.
    private static void ComputeShipRelativeWind(float windDirectionDeg, float shipHeadingDeg, out float angleFromBow, out int side)
    {
        float delta = Mathf.DeltaAngle(shipHeadingDeg, windDirectionDeg); // -180..180
        angleFromBow = 180f - Mathf.Abs(delta);
        // NOTA: sinal derivado analiticamente a partir da mesma fórmula de
        // windSide do ShipMovementSystem - confira em Play Mode comparando
        // com a seta do vento aparente (que usa sailSystem.TrimSide,
        // garantidamente correto) e inverta aqui se estiver espelhado.
        side = delta <= 0f ? 1 : -1;
    }

    // Seta indicando de onde o vento está vindo em relação à proa - convenção
    // de "pra onde o vento empurra o barco" (não a de cata-vento): ↑ = vento
    // vindo de popa (empurrando o barco pra frente), ↓ = vento vindo de proa
    // (vento de proa, contra o barco). Mais fácil de ler de relance que um
    // ângulo de bússola, que exige lembrar pra onde a proa está apontando.
    // Tamanho aumentado (~3.5x) via tag de rich text do TMP - requer "Rich
    // Text" habilitado no componente TextMeshProUGUI (ligado por padrão).
    private static string GetRelativeWindArrow(float angleFromBow, int side)
    {
        string glyph;
        if (angleFromBow <= 20f) glyph = "↓";        // quase direto de proa (Contra) - vento contra o barco
        else if (angleFromBow >= 160f) glyph = "↑";  // quase direto de popa (Popa) - vento empurrando o barco
        else
        {
            bool fromStarboard = side > 0;
            if (angleFromBow < 90f) glyph = fromStarboard ? "\\v" : "v/";       // proa-quarta (Bolina)
            else if (angleFromBow > 90f) glyph = fromStarboard ? "/^" : "^\\";  // popa-quarta (Largo)
            else glyph = fromStarboard ? "->" : "<-";                          // través
        }

        return $"<size=350%>{glyph}</size>";
    }

    private void HandleLogging()
    {
        bool manualLogPressed = Keyboard.current != null && Keyboard.current[manualLogKey].wasPressedThisFrame;

        _logTimer -= Time.deltaTime;
        bool autoLogDue = autoLogEnabled && _logTimer <= 0f;

        if (manualLogPressed || autoLogDue)
        {
            LogSnapshot();
            _logTimer = logInterval;
        }
    }

    private void LogSnapshot()
    {
        Vector3 rudderForce = rudderSystem.GetRudderForce();
        Vector3 localVel = movementSystem.transform.InverseTransformDirection(
            movementSystem.GetComponent<Rigidbody>().linearVelocity);

        StringBuilder sb = new StringBuilder();
        sb.Append("[SHIP_DEBUG] ");
        sb.Append($"t={Time.time:F1} | ");
        if (windSystem != null)
            sb.Append($"wind_dir={windSystem.CurrentDirection:F1} wind_int={windSystem.CurrentIntensity:F1} | ");
        sb.Append($"apparent_dir={movementSystem.ApparentWindDirection:F1} apparent_spd={movementSystem.ApparentWindSpeed:F2} | ");
        sb.Append($"sail_visual_angle={sailSystem.CurrentSailAngle:F1} sail_mode={movementSystem.CurrentSailModeName}({movementSystem.CurrentSailModeIndex}) sail_open={sailSystem.CurrentSailOpenAmount:F2} | ");
        sb.Append($"wind_angle_from_bow={movementSystem.CurrentWindAngleFromBow:F1} in_ideal={movementSystem.CurrentInIdealRange} in_acceptable={movementSystem.CurrentInAcceptableRange} trim_efficiency={movementSystem.CurrentSailEfficiency:F2} sail_force={movementSystem.CurrentSailForce:F0} thrust={movementSystem.CurrentThrustForce:F0} drift={movementSystem.CurrentLateralForce:F0} | ");
        sb.Append($"rudder_angle={rudderSystem.RudderAngle:F1} rudder_force={rudderForce.magnitude:F1} rudder_force_vec={rudderForce} | ");
        sb.Append($"heading={movementSystem.CurrentShipHeadingDeg:F1} speed={movementSystem.CurrentSpeed:F2} local_vel_x={localVel.x:F2} local_vel_z={localVel.z:F2} heel={movementSystem.CurrentHeelAngle:F1} heel_drag_mult={movementSystem.CurrentHeelDragMultiplier:F2} heel_rudder_mult={movementSystem.CurrentRudderHeelEfficiencyMultiplier:F2}");

        Debug.Log(sb.ToString());
    }
}