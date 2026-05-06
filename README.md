# Selfwork Acquiring — .NET 9 Integration

Клиентская библиотека и демо-сервис для интеграции с [Selfwork Эквайринг API](https://docs.selfwork.ru/api/acquiring).

## Структура решения

```
payment/
├── src/
│   ├── Selfwork.Acquiring.Client/   # NuGet-пакет: клиент API
│   └── Selfwork.Acquiring.Web/      # ASP.NET Core 9 демо-сервис
├── tests/
│   └── Selfwork.Acquiring.Tests/    # xUnit тесты (Unit + Integration)
├── docs/
│   └── ТЗ.md                        # Техническое задание
└── CLAUDE.md                        # Контекст проекта для AI-ассистентов
```

## Быстрый старт

### Требования

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- API-ключ и webhook-секрет из личного кабинета [selfwork.ru](https://selfwork.ru)

### Запуск демо-сервиса

```bash
# Клонировать / открыть директорию
cd payment

# Установить ключи (не коммитить в git!)
dotnet user-secrets set "Selfwork:ApiKey" "YOUR_API_KEY" --project src/Selfwork.Acquiring.Web
dotnet user-secrets set "Selfwork:WebhookSecret" "YOUR_WEBHOOK_SECRET" --project src/Selfwork.Acquiring.Web

# Запустить
dotnet run --project src/Selfwork.Acquiring.Web
```

Сервис стартует на `https://localhost:5001`. При запуске автоматически проверяет подключение к Selfwork API.

### Запуск тестов

```bash
dotnet test
```

Все тесты используют WireMock.Net — реальные HTTP-вызовы не выполняются.

---

## Конфигурация

`appsettings.json` / переменные окружения:

| Ключ | Описание | По умолчанию |
|---|---|---|
| `Selfwork:BaseUrl` | Базовый URL API | `https://api.selfwork.ru` |
| `Selfwork:ApiKey` | API-ключ из кабинета | — (обязательно) |
| `Selfwork:WebhookSecret` | Секрет для проверки подписи вебхуков | — (обязательно) |
| `Selfwork:RetryCount` | Кол-во ретраев на HTTP 5xx / сбои | `3` |
| `Selfwork:TimeoutSeconds` | Таймаут одного запроса (сек) | `30` |

Переменные окружения переопределяют `appsettings.json`:

```bash
export Selfwork__ApiKey=prod-key-here
export Selfwork__WebhookSecret=prod-secret-here
```

---

## Подключение библиотеки

### Регистрация в DI

```csharp
// Program.cs
builder.Services.AddSelfworkAcquiring(builder.Configuration);
```

### Использование в сервисе

```csharp
public class OrderService(IAcquiringService acquiring)
{
    public async Task<string> CreatePaymentAsync(decimal amount, string orderId)
    {
        var invoice = await acquiring.CreateInvoiceAsync(new CreateInvoiceRequest
        {
            Amount = (int)(amount * 100), // рубли → копейки
            Description = $"Заказ {orderId}",
            OrderId = orderId,
            SuccessUrl = "https://myshop.ru/payment/success",
            FailUrl = "https://myshop.ru/payment/fail",
        });

        return invoice.PaymentUrl; // → редирект покупателя
    }
}
```

### Проверка статуса

```csharp
var invoice = await acquiring.GetInvoiceAsync(invoiceId);

if (invoice.Status == InvoiceStatus.Paid)
{
    // выполнить заказ
}
```

### Отмена счёта

```csharp
await acquiring.CancelInvoiceAsync(invoiceId);
```

---

## HTTP API демо-сервиса

### Проверка соединения

```http
GET /api/payment/health
```

```json
{
  "connected": true,
  "account": "my-account",
  "verifiedAt": "2026-05-06T10:00:00Z"
}
```

### Создать платёж

```http
POST /api/payment/create
Content-Type: application/json

{
  "amount": 49900,
  "description": "Подписка на месяц",
  "orderId": "order-42",
  "successUrl": "https://myshop.ru/success",
  "failUrl": "https://myshop.ru/fail"
}
```

**Ответ `201 Created`:**

```json
{
  "invoiceId": "inv-abc123",
  "status": "Pending",
  "amount": 49900,
  "paymentUrl": "https://pay.selfwork.ru/inv-abc123",
  "orderId": "order-42",
  "createdAt": "2026-05-06T10:00:00Z",
  "expiresAt": "2026-05-06T11:00:00Z"
}
```

→ Перенаправьте покупателя на `paymentUrl`.

### Статус счёта

```http
GET /api/payment/{invoiceId}/status
```

### Отменить счёт

```http
DELETE /api/payment/{invoiceId}
```

---

## Webhook

Selfwork отправляет `POST` на ваш эндпоинт при смене статуса счёта.

### Настройка в кабинете

Укажите URL: `https://your-domain.ru/api/webhook/payment`

### Формат запроса

```http
POST /api/webhook/payment
X-Webhook-Signature: <hmac-sha256-hex>
Content-Type: application/json

{
  "invoice_id": "inv-abc123",
  "status": "Paid",
  "amount": 49900,
  "order_id": "order-42",
  "event_at": "2026-05-06T10:05:00Z"
}
```

### Верификация подписи

Подпись вычисляется как HMAC-SHA256 над строкой `{invoice_id}:{status_lowercase}:{amount}`:

```csharp
// Проверить подпись вручную (в сервисе это делается автоматически)
verifier.AssertValid(payload, signatureHeader); // бросает WebhookVerificationException при ошибке
```

Контроллер автоматически отклоняет запросы с неверной подписью (`401 Unauthorized`).

---

## Архитектура библиотеки

```
Selfwork.Acquiring.Client/
├── Configuration/
│   └── SelfworkOptions           — параметры (валидируются при старте)
├── Exceptions/
│   ├── AcquiringApiException     — HTTP-ошибки от API (StatusCode + ErrorCode)
│   └── WebhookVerificationException
├── Http/
│   └── AuthHeaderHandler         — DelegatingHandler: вставляет X-Api-Key
├── Models/
│   ├── Requests/                 — CreateInvoiceRequest, InvoiceListQuery
│   └── Responses/                — VerifyResponse, InvoiceResponse, InvoiceStatus...
├── Services/
│   ├── IAcquiringService         — публичный контракт
│   └── AcquiringService          — реализация (internal)
├── Webhook/
│   ├── IWebhookVerifier          — публичный контракт верификации
│   ├── WebhookVerifier           — HMAC-SHA256 (internal)
│   └── PaymentWebhookPayload
└── Extensions/
    └── ServiceCollectionExtensions — AddSelfworkAcquiring(IConfiguration)
```

### Resilience (Polly)

- **Retry**: повтор при HTTP 5xx и `HttpRequestException`, экспоненциальный jitter, настраивается через `RetryCount`
- **Timeout**: на каждый запрос, настраивается через `TimeoutSeconds`
- При `RetryCount = 0` retry-политика не добавляется (полезно для тестов)

---

## Тестирование

Все тесты следуют паттерну **AAA (Arrange / Act / Assert)**:

```csharp
[Fact]
public async Task CreateInvoiceAsync_ValidRequest_ReturnsInvoiceWithPaymentUrl()
{
    // Arrange
    StubPost("/acquiring/invoice", new { id = "inv-001", status = "pending", ... });

    // Act
    var result = await _svc.CreateInvoiceAsync(new CreateInvoiceRequest { Amount = 5000, ... });

    // Assert
    result.Id.Should().Be("inv-001");
    result.PaymentUrl.Should().StartWith("https://");
}
```

**Соглашения:**
- Имена тестов: `Метод_Сценарий_ОжидаемоеПоведение`
- HTTP-уровень мокируется WireMock.Net, интерфейсы — Moq
- `RetryCount=0` в тестовой конфигурации для скорости и детерминизма

### Запуск с покрытием

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## Лицензия

```
MIT License

Copyright (c) 2026 Selfwork Acquiring Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

Свободное использование, модификация и распространение в любых проектах (в том числе коммерческих) при сохранении текста лицензии и указании авторства. Полный текст: [LICENSE](LICENSE)
