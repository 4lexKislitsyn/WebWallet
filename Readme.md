# About
Тестовое задание на должность C# Middle Developer.

# DB

# API

## Transfer

### PUT - Подтверждение операции

Метод подтверждения требует передачу идентификатора кошелька, чтобы невозможно было подтвердить чужой перевод.
Избавиться от этого можно с помощью аутентификации пользователя.

**Проблемы/допущения**:
1. Нет синхронизации списывания, поэтому счет может оказаться отрицательным. Вариантов решения - резервирование средств до подтверждения или удаления операции.
1. Актуальный курс берется в момент подтверждения, поэтому пользователю не показывается действительная стоимость перевода. Если сервис обновления курса будет обновляться только раз в сутки, то проблема встретится при подтверждение в другой день. Возможное решение: 
   - ограничить время жизни операции; фиксировать курс на момент создания; потерпеть возможные потери или прибыли и провести операцию по курсу на момент создания.
   - оставить как есть, и закрывать операцию с актуальным курсом, ведь пользователь в текущей версии не видит актуальный курс при создании операции.
1. Список валют ограничивается только текущим сервисом получения курса валют. При смене сервиса получения курса, не подтвержденные операции могут больше не выполниться, т.к. невозможно будет посчитать курс. Возможное решение:
   - Как и в пункте 1, фиксирование актуального курса на момент создания операции позволило бы уйти от такой проблемы.