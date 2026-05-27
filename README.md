# 🚀 CryptoOrbit

> **Microsserviço de Análise de Criptoativos com IA em Tempo Real**

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple?style=for-the-badge&logo=dotnet)
![Groq AI](https://img.shields.io/badge/AI-Groq%20Llama%203.3-orange?style=for-the-badge)
![CoinGecko](https://img.shields.io/badge/Data-CoinGecko%20API-green?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Completo-brightgreen?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-blue?style=for-the-badge)

O **CryptoOrbit** é um microsserviço inteligente desenvolvido em **ASP.NET Core Web API (.NET 8.0)** que realiza análises financeiras interpretativas de criptoativos em tempo real. Ele vai além das cotações tradicionais ao fundir métricas instantâneas de mercado da **CoinGecko** com relatórios analíticos gerados sob demanda por Inteligência Artificial (**Groq Llama-3.3-70b**).

---
---

<p align="center">
  <img src="blob:https://gemini.google.com/9a458115-5ec0-4d02-957d-14d17e82f42f" alt="Arquitetura de Fluxo de Dados e Gestão de Cache do CryptoOrbit" width="90%">
</p>

---

## 🎯 Visão Geral

### Diferenciais Estratégicos

| Recurso | Descrição |
|---|---|
| 🤖 **Fusão de Cotações com IA** | Automatiza a interpretação de flutuações complexas de preços para o usuário final |
| 📊 **Micro-Decisões de Sentimento** | Classifica instantaneamente o cenário do ativo: Alta, Correção ou Lateralização |
| ⚡ **Mitigação de Latência** | Cache proativo em segundo plano garante respostas em escala de milissegundos |

---

## 🏗 Arquitetura

A API adota práticas de **Clean Architecture** com forte desacoplamento via injeção de dependência por interfaces bem definidas.

```
[ Cliente / Requisição HTTP ]
│
├──► [ CriptoController ]
│         │
│         ▼
│    [ ICripto / CriptoServices ]
│         │
│         ├─► IMemoryCache ──(HIT)──► Retorna JSON em milissegundos
│         │
│         └─► (Cache MISS / Expirado)
│               │
│               ├─► CoinGecko API  (Preço, Volume, Variação 24h)
│               │
│               └─► IGroqInterface / GroqServices
│                     │
│                     ▼  (Temp: 0.1 | JSON Mode)
│               [ Groq AI Llama-3.3 ]
│
▼ (A cada 12 horas)
[ CriptoCacheBackgroundService ]
└─► Top 20 Moedas ──► Atualiza IMemoryCache
```

---

## 📁 Estrutura do Projeto

```
📁 CryptoOrbit/
│
├── 📁 Configurations/
│   └── ExternalServicesOptions.cs       # Mapeamento de opções de serviços externos
│
├── 📁 Controller/
│   └── CriptoController.cs              # Endpoints RESTful unificados
│
├── 📁 Dtos/
│   └── CriptoDto.cs                     # Higienização e transporte seguro de payloads
│
├── 📁 Interfaces/
│   ├── ICripto.cs                       # Contrato do serviço de criptoativos
│   └── IGroqInterface.cs                # Abstração para testes e mocks da IA
│
├── 📁 Models/
│   └── ResponseCoins.cs                 # Modelos de binding de respostas externas
│
└── 📁 Services/
    ├── CriptoServices.cs                # Orquestração de dados e montagem de prompts
    ├── GroqServices.cs                  # Comunicação HTTP segura com a API do Groq
    └── CriptoCacheBackgroundService.cs  # Worker para pré-processamento de cache
```

---

## ⚙️ Recursos de Engenharia e Performance

### 🛡️ Throttling Control (Prevenção de Rate Limits)
Para a listagem em lote (`GetAllCoinsWithAnalysisAsync`), o serviço introduz uma janela controlada de **7 segundos** entre chamadas sequenciais ao LLM, blindando a aplicação contra bloqueios HTTP 429 na API da Groq.

### 🎯 Determinismo na Resposta da IA
O modo JSON nativo do modelo é forçado via `response_format = { type: "json_object" }` com temperatura calibrada em **0.1**, eliminando alucinações e garantindo conversão perfeita para o `CriptoDto`.

### ⚡ Cache Proativo
O `CriptoCacheBackgroundService` atualiza o `IMemoryCache` a cada **12 horas** para as 20 criptomoedas configuradas no `appsettings.json`, reduzindo o tempo de resposta de segundos para milissegundos.

---

## 🔗 API Reference

**Base URL:** `/api/Cripto`

### 🔐 Headers Obrigatórios

| Header | Tipo | Descrição |
|---|---|---|
| `x-cg-demo-api-key` | `string` | Chave de autenticação da plataforma CoinGecko |
| `X-Groq-Key` | `string` | Token de acesso à API da Groq AI |

---

### `GET /api/Cripto/get-all-coins`

Retorna um array com o relatório completo pré-analisado das **20 principais criptomoedas** do mercado mundial.

**Resposta:** `HTTP 200 OK` — Array de `CriptoDto`

---

### `GET /api/Cripto/{nameCoin}`

Retorna a análise preditiva e o sentimento mercadológico de uma moeda específica.

**Parâmetros:**

| Parâmetro | Tipo | Exemplos |
|---|---|---|
| `nameCoin` | `string` | `bitcoin`, `ethereum`, `solana`, `btc` |

**Exemplo de Resposta (`HTTP 200 OK`):**

```json
{
  "name": "Bitcoin",
  "symbol": "btc",
  "image": "https://assets.coingecko.com/coins/images/1/large/bitcoin.png",
  "current_price": 68450.00,
  "high_24h": 69200.00,
  "low_24h": 67800.00,
  "price_change_percentage_24h": 1.72,
  "price_range": 1400.00,
  "total_volume": 28495028450,
  "recommendation": "O ativo Bitcoin (btc) apresenta um cenario de tendencia de alta nas ultimas 24 horas, acumulando uma variacao de 1.72%. Com o preco atual cotado em 68450, o ativo registrou uma oscilacao diaria entre a minima de 67800 e a maxima de 69200, movimentando um volume total de 28495028450 no mercado."
}
```

---

## 🔌 Guia de Integração

### JavaScript / TypeScript (React, Vue, Vanilla)

```javascript
const fetchCoinAnalysis = async (coinName) => {
  const url = `https://seu-dominio.com/api/Cripto/${coinName}`;

  try {
    const response = await fetch(url, {
      method: 'GET',
      headers: {
        'Accept': 'application/json',
        'x-cg-demo-api-key': 'SUA_CHAVE_COINGECKO_AQUI',
        'X-Groq-Key': 'SUA_CHAVE_GROQ_AQUI'
      }
    });

    if (!response.ok) throw new Error(`Erro na API: ${response.status}`);

    const data = await response.json();
    console.log("Dados do Ativo Enriquecidos:", data);
    return data;
  } catch (error) {
    console.error("Falha ao buscar dados:", error);
  }
};
```

### Python (Scripts de Trading / Data Science)

```python
import requests

def get_crypto_analysis(coin_name: str, cg_key: str, groq_key: str):
    url = f"https://seu-dominio.com/api/Cripto/{coin_name}"
    headers = {
        "x-cg-demo-api-key": cg_key,
        "X-Groq-Key": groq_key,
        "Accept": "application/json"
    }

    try:
        response = requests.get(url, headers=headers)
        response.raise_for_status()
        data = response.json()
        print(f"Recomendação para {data['name']}: {data['recommendation']}")
        return data
    except requests.exceptions.HTTPError as err:
        print(f"Erro na requisição: {err}")
```

### C# (.NET HttpClient)

```csharp
using System.Net.Http.Headers;
using System.Text.Json;

public class CryptoClient
{
    private readonly HttpClient _httpClient;

    public CryptoClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<CriptoDto> FetchAnalysisAsync(string coinName, string cgKey, string groqKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"api/Cripto/{coinName}");
        request.Headers.Add("x-cg-demo-api-key", cgKey);
        request.Headers.Add("X-Groq-Key", groqKey);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CriptoDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
```

---

## 🚀 Como Executar

**Pré-requisitos:** .NET 8.0 SDK, chave CoinGecko e chave Groq AI.

```bash
# 1. Clone o repositório
git clone https://github.com/seu-usuario/CryptoOrbit.git
cd CryptoOrbit

# 2. Configure as variáveis no appsettings.json ou via ambiente
# CoinGeckoApiKey, GroqApiKey e lista das top 20 moedas

# 3. Restaure as dependências e execute
dotnet restore
dotnet run
```

A API estará disponível em `https://localhost:5001/api/Cripto`.


---

<p align="center">Feito com ☕ e .NET</p>
