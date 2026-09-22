# NTUNHS Pet Mate

本系統為國立臺北護理健康大學智慧照護科技應用課程專案。

Member：

- @peng940211
- @wiiba

## 虛擬寵物情緒助手（AI Pet Mood Assistant）

這是一個基於 C# Windows Forms 開發的桌面虛擬陪伴系統。專案結合了本地端生成式 AI 模型與 SQLite 資料庫，將傳統的電子寵物體驗升級為具備自然語言理解與情緒追蹤的互動助手。

### 核心功能架構

- **本地端 AI 對話引擎**
  - 透過 HTTP POST 串接本地端量化 LLM（預設連接 `127.0.0.1:1234`）。
  - 實作 Prompt Engineering 限制模型扮演「溫柔的貓咪情緒助手」，並自動過濾推理標籤（`<think>`），提供沉浸式的陪伴體驗。
  - 內建簡繁轉換邏輯，確保對話介面的一致性。
- **動態視覺與互動介面**
  - 支援「餵食」、「玩樂」、「休息」等互動按鈕。
  - 系統會根據使用者的操作，從素材庫中動態隨機抽取並載入對應的 GIF 動畫，保持視覺回饋的新鮮感。
- **情緒追蹤與視覺化**
  - 內建 SQLite 輕量級資料庫（`pet.db`）自動生成與連線機制。
  - 即時記錄使用者的心情互動分數，並透過內建 Chart 模組自動繪製近期情緒波動折線圖。

### 技術堆疊

- **前端介面**：C# .NET Framework 4.7.2 (Windows Forms)
- **資料儲存**：SQLite (`System.Data.SQLite`)
- **AI 串接**：`HttpClient` + `Newtonsoft.Json` (相容 OpenAI API 格式)
- **文字處理**：`Microsoft.VisualBasic.Strings.StrConv` (繁簡轉換)

### 快速啟動指南

1. **還原專案**：使用 Visual Studio 開啟 `寵物.sln`，系統會依據 `packages.config` 自動還原 NuGet 套件。
2. **啟動 AI 伺服器**：請確保本機端已運行支援 OpenAI API 格式的 LLM 伺服器（如 LM Studio 或 Ollama），並監聽 Port `1234`。
3. **編譯運行**：按下 `F5` 啟動專案，資料庫 `pet.db` 將於首次啟動時自動建立。
