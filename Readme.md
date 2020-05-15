# О проекте

Тестовое задание на должность C# Middle Developer.

## База данных

При запуске отладочной среды используется класс на основе списков и словарей. Он используется в тестах, если не было выполнено подмены на объект заглушку. Его можно перенести в проект тестов и использовать при обычном запуске подключение к БД.

В качестве используемой базы данных взята MySql. Работа с БД выполняется с помощью EFCore.

## API

Пользователю предоставляется API для создания кошелька и выполнения переводов. Больше информации об API можно получить по адресу `~/swagger`.

## Создание и просмотр кошелька

- Создание кошелька `POST api/wallet`.
- Просмотра кошелька `GET api/wallet/id`.

## Переводы средств

Методы подтверждения/удаления/получения перевода требуют передачу идентификатора кошелька, чтобы невозможно было выполнить действие с переводом принадлежащему другому пользователю.
Избавиться от этого можно с помощью аутентификации пользователя.

- Создание перевода `POST api/transfer` с передачей исходной и целевой валюты
- Пополнение кошелька `POST api/transfer` с передачей целевой валюты
- Снятие средств `POST api/transfer` с передачей валюты для снятия

После создания операцию можно подтвердить или удалить. Перевод средств выполняется только после подтверждения.

Для просмотра операции нужно выполнить запрос `GET api/transfer/id?walletId=transferWalletId`

### Проблемы/допущения

1. Нет синхронизации списывания, поэтому счет может оказаться отрицательным. **Возможное решение**: резервирование средств до подтверждения или удаления операции.
1. Актуальный курс для расчета берется в момент подтверждения, поэтому пользователь не видит действительный курс перевода. Если сервис обновления курса будет обновляться только раз в сутки, то проблема встретится при подтверждение в другой день. **Возможное решение**:
   - ограничить время жизни операции; фиксировать курс на момент создания (уже фиксируется); потерпеть возможные потери или прибыли и провести операцию по курсу на момент создания.
   - оставить как есть, и закрывать операцию с актуальным курсом.
1. Список валют при переводе между счетами ограничивается только текущим сервисом курса . При смене сервиса, не подтвержденные операции могут больше не выполниться, т.к. невозможно будет посчитать курс. Возможное решение:
   - Как и в пункте 1, фиксирование актуального курса на момент создания операции.
1. Переполнение баланса (тест `UnitTests.TransfersControllerTests.BalanceOverflow`). Но надо быть очень богатым, чтобы такое случилось. Если баланс достиг максимум для `double` все остальные переводы, можно сказать уйдут в пустоту.
1. Нет ограничений на валюты при создании, т.о. пользователь может создать баланс на неизвестную сервису конвертации валюту. Из этой валюты будет невозможен перевод на другие счета, до тех пор пока это не будет позволять сервис подсчет курса. Это может быть как полезно, так и бесполезно. Текущее решение по этому вопросу - оставить эту возможность. **Возможные решения**:
   - хранить в конфигурации поддерживаемые валюты;
   - ограничивать списком валют сервиса

## Замена сервиса получения курса валют

### Без написания кода

Замену сервиса можно произвести без сборки проекта, если формат другого сервиса совпадает с сервисом [ECB](https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml). 

*Если нужна большая универсальности, можно добавить в конфигурацию три свойства*:

- XPath запрос для выбора элементов
- XPath запрос для выбора идентификатора курса
- XPath запрос для получения значения текущего курса

и использовать эти свойства при получении данных вместо десериализации.

В таком случае можно было бы использовать любой сервис, который отдает курс валют относительно условной единицы в формате XML.

### С написанием кода

1. Написать класс реализующей интерфейс `ICurrencyRateService`;
2. В методе `Startup.ConfigureServices` поменять регистрацию 

```diff
- services.AddTransient<ICurrencyRateService, ECBCurrencyRateService>();
+ services.AddTransient<ICurrencyRateService, YouNewCurrencyRateService>();
```

## Модульные тесты

Покрытие тестами:

- [x] создание кошелька
- [x] получение кошелька
- [x] создание перевода
- [x] удаление перевода
- [ ] подтверждение перевода
- [ ] получение перевода
- [x] сервис получения курса валют
- [x] преобразования с помощью AutoMapper

## Доработки

### Логирование

Логирование выполняется только в консоль. В реальном проекте рекомендуется добавлять сторонние библиотеки для записи логов в БД и/или файлы. Для этого можно использовать библиотеку [NLog](https://nlog-project.org/). Данная библиотека:

- поддерживает запись в БД и файлы
- имеет большое количество встроенных отображений
- совместима с .net core и DI
- позволяет добавить свои классы для отображений

### Polly

Для обработки ошибок сервиса валют при выполнении запросов можно использовать библиотеку [Polly](https://github.com/App-vNext/Polly). Она позволяет добавлять различные политики повтора запросов и избежать выполнения лишних запросов при неработающем сервисе.