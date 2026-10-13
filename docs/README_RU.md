### [> Русский <](./README_RU.md) | [English](./README.md) | [简体中文](./README_ZH.md)
---

<div align="center">
  <h1>EnhancedCarTuning</h1>
  <h3>BepInEx плагин для Steam-версии игры <b>MadOut2 BigCityOnline </b> v9.4, портирующий тюнинг автомобилей из более новых мобильных версий игры.</h3>
  <h3>Для работы плагина требуется <a href="https://github.com/LxgacyTeam/BigCityLegacy">BigCityLegacy</a></h3>
</div> 

---

## Возможности:
* Выбор двигателя и привода (RWD, FWD, AWD)
* Раздельная покраска деталей кузова (бампер, капот, багажник, двери)
* Гибкая настройка подвески: высота и вылет колес, ход, жесткость, развал
* Настройка радиуса и ширины колес
* Раздельная настройка передней и задней оси
* Текстовые винилы с выбором шрифта
* Симметрия наклеек
* Улучшенные текстуры фигур
* Увеличенное разрешение полотна винилов (с возможностью настройи)
* Активация полицейского скина для автомбилей **Bob**, **Gelik**, **Baklajan** с синхронизацией мигалок в онлайне
* Полная синхронизация тюнинга в онлайне

---

## Установка и требования:
Требуется оригинальная игра **MadOut2 BigCityOnline версии 9.4** с установленным [**BigCityLegacy**](https://github.com/LxgacyTeam/BigCityLegacy) версии не ниже 1.4.0

### Вариант 1:
Воспользуйтесь [онлайн-установщиком BigCityLegacy](https://github.com/LxgacyTeam/BCL-installer/releases/latest/download/BigCityLegacy_Installer.exe): на этапе выбора дополнительных плагинов установите галочку на "EnhancedCarTuning" и пройдите установку.

### Вариант 2:
Скачайте архив из раздела [Releases](https://github.com/LxgacyTeam/EnhancedCarTuning/releases) и распакуйте в корневую папку игры.

---

## Сборка:
**Требования:**
- MadOut2 9.4 с установленным BigCityLegacy
- .NET SDK

**Процесс сборки:**
Клонируйте и соберите проект, указав **абсолютный путь** к корневой папке игры:

```bat
git clone https://github.com/LxgacyTeam/EnhancedCarTuning.git
cd EnhancedCarTuning
dotnet build -c Release -p:GameDir="path\to\game\dir" -p:CopyToPlugins=true
```

> [!NOTE]
> При открытии проекта в Visual Studio **обязательно** укажите путь к корневой папке игры в атрибуте `<GameDir>` в файле `Directory.Build.props`. После все необходимые Unity и BepInEx сборки подключатся автоматически

---

## Условия и положения

EnhancedCarTuning распространяется в рамках проекта BigCityLegacy и наследует все его условия использования, а также применяемые правовые нормы
См. [NOTICE](https://github.com/LxgacyTeam/BigCityLegacy/blob/main/docs/NOTICE_RU.md).