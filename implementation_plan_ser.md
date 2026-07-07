# Implementation Plan

[Overview]
Цель: Исправить сериализацию словаря контрольных сумм (используемого для создания Zorro.json) так, чтобы объекты ArchiveFileInfo сериализовались с полиморфным дискриминатором ($type) и сохраняли свои специфичные поля.

Контекст и подход: При сериализации ConcurrentDictionary<string,IList<ExtendedFileInfo>> System.Text.Json не получает метаданные полиморфизма для базового типа ExtendedFileInfo, поэтому сложные наследники (например ArchiveFileInfo) записываются как поля базового типа без $type. Решение — зарегистрировать в source‑generator (JsonSerializerContext) необходимые корневые типы и базовый тип ExtendedFileInfo, а также централизовать вызов сериализации с опцией TypeInfoResolver = ArchiveJsonContext.Default (или явно передавать сгенерированный JsonTypeInfo). После внесения изменений нужно добавить unit‑тест, который проверит появление дискриминатора и наличие полей ArchiveFileInfo в выходном JSON.

[Types]
Изменения в типах: добавить сериализационные метаданные для ExtendedFileInfo и корневых контейнерных типов в ArchiveJsonContext.

Подробно:
- ExtendedFileInfo (базовый тип): сохраняет атрибуты полиморфизма уже выставлены ([JsonPolymorphic], [JsonDerivedType(...)]). Требуется регистрация типа в JsonSerializerContext чтобы генератор создал JsonTypeInfo с поддержкой дискриминатора.
- ArchiveFileInfo (наследник): discriminator = "archive" (как задано в ExtendedFileInfo.cs).
- Корневой сериализуемый тип: ConcurrentDictionary<string,IList<ExtendedFileInfo>> (и/или Dictionary<string,IList<ExtendedFileInfo>> для надёжности). Для корректной генерации нужно указать все используемые контейнерные типы: IList<ExtendedFileInfo>, ExtendedFileInfo[], Dictionary<string,IList<ExtendedFileInfo>>, ConcurrentDictionary<string,IList<ExtendedFileInfo>>.
- Валидции: при тестировании проверять наличие поля "$type":"archive" и специфичных полей ArchiveFileName / ArchivePath.

[Files]
Изменения в файлах: добавить/изменить конкретные файлы (полные пути).

- Новые или изменяемые файлы:
  - Изменить: DupTerminator.DataBase/ArchiveJsonContext.cs
    - Добавить JsonSerializable‑атрибуты:
      - [JsonSerializable(typeof(ExtendedFileInfo))]
      - [JsonSerializable(typeof(ArchiveFileInfo))]
      - [JsonSerializable(typeof(IList<ExtendedFileInfo>))]
      - [JsonSerializable(typeof(Dictionary<string, IList<ExtendedFileInfo>>))]
      - [JsonSerializable(typeof(ConcurrentDictionary<string, IList<ExtendedFileInfo>>))]
    - Цель: заставить source‑generator создать TypeInfo для базового типа и для контейнерных типов.
  - Изменить: BusinessLogic/SearcherMD5Container.cs
    - В местах, где формируется/сохраняется JSON (пример закомментированного кода на строках ~74–80), заменить прямой вызов JsonSerializer.Serialize(...) на вызов с опциями, использующими TypeInfoResolver = ArchiveJsonContext.Default, либо на центральный helper (см. ниже).
  - Изменить (рекомендация): DupTerminator.WPF/Helper/SerializeHelper.cs
    - Если там есть общие вспомогательные сериализации, переключить на использование централизованного метода сериализации с ArchiveJsonContext.
  - Изменить (при необходимости): любые другие места, где сериализуется ConcurrentDictionary<string,IList<ExtendedFileInfo>> (поиск по проекту), заменить на новый helper или передавать options/TypeInfoResolver.

- Новые файлы:
  - (опционально) DupTerminator.DataBase/SerializationHelpers.cs — центральный helper для сериализации/десериализации checksum‑dictionary:
    - SerializeChecksumDictionary(...): string / Stream — создает JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), WriteIndented = true, TypeInfoResolver = ArchiveJsonContext.Default } и вызывает JsonSerializer.Serialize.
    - Deserialize...: использовать JsonSerializer.Deserialize с теми же опциями/контекстом.

- Файлы, которые не удаляются/перемещаются.

[Functions]
Описание функций для добавления/изменения.

- Новые функции:
  - public static string SerializeChecksumDictionary(ConcurrentDictionary<string,IList<ExtendedFileInfo>> dict)
    - Файл: DupTerminator.DataBase/SerializationHelpers.cs (новый)
    - Сигнатура: string SerializeChecksumDictionary(ConcurrentDictionary<string,IList<ExtendedFileInfo>> dict)
    - Назначение: централизовать создание JsonSerializerOptions с TypeInfoResolver = ArchiveJsonContext.Default + encoder + форматирование, возвращать JSON строку или записывать в поток.
  - public static void SerializeChecksumDictionaryToFile(ConcurrentDictionary<string,IList<ExtendedFileInfo>> dict, string path)
    - В том же файле, вызывает SerializeChecksumDictionary и записывает результат в файл (или использует JsonSerializer.Serialize с потоком и опциями).

- Модифицированные функции:
  - В BusinessLogic/SearcherMD5Container.cs:
    - Место: участок где в примере закомментирован вызов JsonSerializer.Serialize (строки около 74–80).
    - Заменить примерный вызов на:
      var opts = new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), WriteIndented = true, TypeInfoResolver = ArchiveJsonContext.Default };
      var s = JsonSerializer.Serialize(checksumDictionary, opts);
      File.WriteAllText("Zorro.json", s);
    - Альтернатива: использовать SerializeChecksumDictionaryToFile helper.
  - (Опционально) В DupTerminator.DataBase/JsonHelper.cs
    - Методы CompressJsonData/CompressJsonDataToStream принимают JsonSerializerOptions — документация уже поддерживает. Рекомендуется при их вызове передавать опции с TypeInfoResolver = ArchiveJsonContext.Default.

- Удаляемые функции: нет.

[Classes]
Изменения в классах: минимальные, в основном конфигурация контекста.

- Modified class:
  - DupTerminator.DataBase.ArchiveJsonContext (partial) — расширить атрибутами JsonSerializable (см. Files).
- New class:
  - (optional) DupTerminator.DataBase.SerializationHelpers — статический класс с двумя методами сериализации/десериализации, как описано выше.

[Dependencies]
Изменения зависимостей: не требуются новые пакеты.
- Требуется: проект должен использовать .NET 6+ / System.Text.Json source generators (уже используется ArchiveJsonContext, значит окружение подходит).
- Не требуется добавлять сторонние пакеты.

[Testing]
Подход к тестированию: добавить модульные тесты в проект DupTerminator.Test, проверяющие, что сериализация включает дискриминатор и специфичные поля.

Требования к тестам:
- Файл: DupTerminator.Test/SerializationTests.cs (новый) или добавить в DupTerminator.Test/SearcherMD5ContainerTests.cs.
- Тест 1: Serialize_ChecksumDictionary_IncludesDiscriminator
  - Создать ConcurrentDictionary<string,IList<ExtendedFileInfo>> с одной записью, где IList содержит один ArchiveFileInfo (заполнить ArchiveFileName/ArchivePath/ArchiveExtension).
  - Вызвать DupTerminator.DataBase.SerializationHelpers.SerializeChecksumDictionary или напрямую JsonSerializer.Serialize(dict, new JsonSerializerOptions { TypeInfoResolver = ArchiveJsonContext.Default, Encoder=..., WriteIndented=true }).
  - Assert: jsonString содержит "\"$type\":\"archive\"" и содержит "ArchiveFileName" и значение, которое указали.
- Тест 2: Deserialize_ChecksumDictionary_RestoresArchiveFileInfo
  - Взять jsonString из предыдущего шага и десериализовать обратно в Dictionary/ConcurrentDictionary с теми же опциями и проверить, что элемент типа ArchiveFileInfo (runtime type) и что ArchiveFileName восстановлен.
- Тест 3 (интеграционный, опционально): использовать существующий TestFactory.LoadFromJson чтобы убедиться, что он корректно распознаёт ArchiveFileInfo из ранее созданного JSON.

Валидация: тесты должны выполняться в CI/локально и проходить без изменений в runtime.

[Implementation Order]
Последовательность действий:

1. Добавить/обновить атрибуты JsonSerializable в DupTerminator.DataBase/ArchiveJsonContext.cs:
   - Добавить перечисленные typeof(...) для ExtendedFileInfo, ArchiveFileInfo, IList<ExtendedFileInfo>, Dictionary<string,IList<ExtendedFileInfo>>, ConcurrentDictionary<string,IList<ExtendedFileInfo>>.
2. Создать DupTerminator.DataBase/SerializationHelpers.cs с методами SerializeChecksumDictionary и SerializeChecksumDictionaryToFile (использовать ArchiveJsonContext.Default в JsonSerializerOptions.TypeInfoResolver).
3. Заменить все места сериализации checksumDictionary (в частности в BusinessLogic/SearcherMD5Container.cs пример кода и/или другие найденные вызовы) на использование нового helper либо на JsonSerializer с опциями TypeInfoResolver = ArchiveJsonContext.Default.
4. Добавить unit‑тест(ы) DupTerminator.Test/SerializationTests.cs с тестами, которые проверяют появление "$type":"archive" и восстановление типа при десериализации.
5. Собрать проект и запустить юнит‑тесты, исправить возникшие ошибки сборки (включая отсутствие using или доступность типов).
6. Пересоздать Zorro.json (пользователь выполнил это отдельно) и убедиться, что записи теперь содержат дискриминатор и поля ArchiveFileInfo.

Дополнительные заметки и риски:
- Если проект использует AOT/требует специфической конфигурации для source‑generator, убедиться, что ArchiveJsonContext будет собран и доступен (пересборка решит).
- В некоторых вызовах сериализации могли использоваться JsonSerializer.Serialize(gzipStream, data, options) — убедиться, что в этих местах передаётся тот же options с TypeInfoResolver.
- Для надёжности тестов избегать зависимости от конкретных свойствах имени сгенерированного JsonTypeInfo; полагаться на строку дискриминатора "$type":"archive" и наличие ключевых полей.