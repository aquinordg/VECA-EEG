---
description: Verifica o estado atual do projeto VECA-EEG — scripts implementados, pendências de configuração Unity, e próximos passos recomendados.
---

Você é um assistente especialista no projeto VECA-EEG (Unity VR + eye-tracking + EEG).

Execute as seguintes verificações e apresente um relatório estruturado em português (pt-BR):

## 1. Scripts implementados

Leia os arquivos em `Assets/_VECA-EEG/Scripts/` e liste cada script com:
- Nome do arquivo
- Classe principal e se herda TaskBase (para tasks)
- Status: Completo / Incompleto / Com TODO pendente

## 2. Consistência entre TestManager e Tasks

Leia `Assets/_VECA-EEG/Scripts/Core/TestManager.cs` e verifique:
- Quais tasks estão referenciadas como campos `public`
- Se cada task referenciada tem um script correspondente em `Tasks/`
- Se a sequência de execução bate com a ordem do artigo: Memória → Atenção → Abstração → Cálculo → Execução → Recall
- Se `uiManager.HideStartScreen()` é chamado no início da sequência
- Se `records` é do tipo `List<TrialRecord>` (não mais `Dictionary<string, float>`)
- Se `eyeTracker.CurrentTrialLabel` é definido antes de cada bloco de tarefa
- Se `LSLMarkerStream.Instance?.SendMarker(...)` é chamado com `session_start` e `session_end`

## 3. Fluxo de introdução e feedback

Verifique em cada task:
- Se `IntroPhase()` é chamado no início de `RunAllTrials()` (ou equivalente)
- Se `taskDescription` tem valor padrão definido em `Awake()`
- Se o feedback (`ShowFeedback` / `HideFeedback`) é exibido após cada trial
- Se `ShowAOIs(false)` é chamado ao final de cada trial

## 4. UIManager

Leia `Assets/_VECA-EEG/Scripts/UI/UIManager.cs` e confirme:
- Campos presentes: `gotItButton`, `startScreenPanel`, `introText`, `gotItHint`, `introMessage`
- Métodos: `WaitForConfirmation()`, `ConfirmUnderstood()`, `HideStartScreen()`
- Se `gotItHint` usa `<b>` (negrito) — não `<i>` (itálico)
- Se `introMessage` termina com a frase "INICIAR TESTE" em negrito

## 5. Exportação CSV

Leia o método `SalvarCSV()` em TestManager.cs e verifique se as colunas exportadas batem exatamente com:
`participant_id, trial_start, trial_end, feature, value`

E se as features são gravadas na ordem:
`vr_mem8, vr_mem9, vr_mem10, vr_att, vr_abs, vr_calc4, vr_calc5, vr_calc6, vr_exec, vr_recall`

Verifique também:
- Se os timestamps usam `CultureInfo.InvariantCulture` (ponto decimal, não vírgula)
- Se o formato de `trial_start`/`trial_end` inclui milissegundos (`yyyyMMdd_HHmmss.fff`)
- Se há fallback para `default` (campo não preenchido) nos timestamps

## 6. EyeTracker

Leia `Assets/_VECA-EEG/Scripts/Core/EyeTracker.cs` e confirme:
- Se usa `InputAction` com binding `<EyeGaze>/pose` (OpenXR Eye Gaze Interaction) — não mais mouse
- Se `TryGetGazeRay()` é `public` e lê `PoseState` verificando `isTracked` **ou** `trackingState` flags (Position|Rotation) como fallback — necessário para Vive Pro Eye cujo driver não seta `isTracked`
- Se `ObterPosicaoGaze()` é `public` e faz fallback para mouse quando gaze não rastreado
- Se o limiar de fixação usa **distância angular** (graus entre frames, `limiarAngularGraus ≤ 1.5°`) por ≥ `duracaoMinimaFixacao` (0.12s) — não mais pixels de tela
- Se as propriedades `RecordingStartTime` e `RecordingEndTime` (System.DateTime) existem
- Se `RecordingStartTime` é atribuído em `StartRecording()` e `RecordingEndTime` em `StopRecording()`
- Se `gazeAction` é criado em `Awake()` e descartado em `OnDestroy()`
- Se há `Debug.LogWarning` em `Start()` quando `gazeAction.controls.Count == 0` (aviso acionável, não log de contagem)
- Se a propriedade `CurrentTrialLabel` (string, get/set) existe
- Se `StartRecording()` envia `LSLMarkerStream.Instance?.SendMarker($"trial_start,{CurrentTrialLabel}")`
- Se `StopRecording()` envia `LSLMarkerStream.Instance?.SendMarker($"trial_end,{CurrentTrialLabel}")`

## 7. Integração LSL

Leia `Assets/_VECA-EEG/Scripts/Core/LSLMarkerStream.cs` e confirme:
- Se é um singleton (campo estático `Instance`, inicializado em `Awake()`)
- Se cria outlet LSL com nome `"VECA-Markers"`, tipo `"Markers"`, canal único
- Se degrada silenciosamente (try/catch) quando `lsl.dll` está ausente
- Se o método público `SendMarker(string marker)` existe e envia string via outlet

Leia `Assets/_VECA-EEG/Scripts/Core/LSLNative.cs` e confirme:
- Se usa P/Invoke para `lsl.dll` (liblsl v1.17)
- Se expõe as classes `StreamInfo` e `StreamOutlet` no namespace `LSL`

## 8. Timestamps por trial (sincronização EEG)

Verifique em cada task multi-trial (MemoryTask, AbstractionTask, CalculationTask, RecallTask):
- Se há arrays `_trialStartTimes[]` e `_trialEndTimes[]` do tipo `System.DateTime`
- Se os arrays são preenchidos de `eyeTracker.RecordingStartTime` / `RecordingEndTime` logo após `StopRecording()`
- Se há getter público `GetTrialTimes(int idx)` retornando tupla `(start, end)`

Verifique em TaskBase:
- Se `TrialStartTime` e `TrialEndTime` existem como propriedades públicas
- Se são atribuídos de `eyeTracker.RecordingStartTime` / `RecordingEndTime` em `ExecutionPhase()` (cobre AttentionTask e ExecutionTask)

## 9. MemoryTask e RecallTask

Verifique:
- Se `MemoryTask` tem campo `sprites` (pool único, mínimo 4)
- Se `InicializarOrdem()` sorteia 3 alvos e o restante vira distratores
- Se `GetTargetSprite(int)`, `GetTargetLabel(int)`, `GetDistractorSprites()` e `GetTrialTimes(int)` estão implementados
- Se `RecallTask` tem referência pública `memoryTask` e usa os getters acima
- Se `RecallTask.ExecutarUmTrial()` exibe o ordinal ("primeira", "segunda", "terceira") na instrução

## 10. CalculationTask

Verifique:
- Se o método `Reset()` (botão Reset no Inspector) tem `"93 − 7 ="` no trial 1 (não `"93 − 5 ="`)
- Se `aleatorio = true` por padrão
- Se `GetTrialTimes(int)` está implementado

## 11. Gaze Cursor e Dwell Button

Leia `Assets/_VECA-EEG/Scripts/UI/GazeCursor.cs` e confirme:
- Se usa `eyeTracker.TryGetGazeRay()` para posicionamento world-space
- Se faz fallback para `vrCamera.transform.forward` quando gaze não rastreado (cursor nunca some)
- Se usa `cursorRenderer.enabled` para mostrar/ocultar (não `SetActive`)

Leia `Assets/_VECA-EEG/Scripts/UI/GazeDwellButton.cs` e confirme:
- Se detecta sobreposição via `RectTransformUtility.RectangleContainsScreenPoint`
- Se usa `eyeTracker.vrCamera` como câmera de referência
- Se `activated` é resetado em `OnEnable()` (permite reuso em múltiplas telas)
- Se `fillImage.fillAmount` é atualizado proporcionalmente ao progresso do dwell

## 12. Pendências de configuração Unity

Com base nos campos `public` encontrados nos scripts, liste:
- GameObjects que precisam existir na Hierarchy
- Campos do Inspector que precisam ser preenchidos manualmente
- Referências entre componentes (ex: RecallTask → MemoryTask)
- Eventos OnClick que precisam ser configurados nos botões
- Se o componente **LSLMarkerStream** está no mesmo GameObject do TestManager
- Se `Assets/Plugins/lsl.dll` existe (liblsl v1.17 — não versionado, deve ser instalado manualmente)

## 13. Próximos passos recomendados

Sugira no máximo 3 próximos passos ordenados por prioridade, considerando o que está faltando.

---

Apresente o relatório em seções com ícones de status: ✅ OK, ⚠️ Atenção, ❌ Problema.
