# Использование API для фильтрации метрик

## Стратегия выбора API

### REST API - для фильтров по дате
**Используйте REST API когда:**
- ✅ Есть фильтры по дате (`fromDate`, `toDate`)
- ✅ Нужны детальные данные с `payload`
- ✅ Большие диапазоны дат

**Endpoint:** `GET /api/metrics`

**Пример:**
```typescript
// Frontend
this.restService.getAllMetrics({
  type: 'temperature',
  name: 'Location1',
  fromDate: new Date('2025-01-01'),
  toDate: new Date('2025-01-31')
}, 100);
```

**Параметры:**
- `type` (optional) - фильтр по типу
- `name` (optional) - фильтр по имени/локации
- `fromDate` (optional) - начальная дата (ISO string)
- `toDate` (optional) - конечная дата (ISO string)
- `page` (default: 1) - номер страницы
- `pageSize` (default: 10) - размер страницы

### GraphQL - для фильтров без дат
**Используйте GraphQL когда:**
- ✅ Фильтры только по `type` и `name` (без дат)
- ✅ Агрегации и статистика
- ✅ Последние записи без фильтров

**Пример:**
```typescript
// Frontend - только type и name, без дат
this.graphQLService.getMetrics(20, undefined, {
  type: 'temperature',
  name: 'Location1'
  // НЕТ fromDate/toDate - используем GraphQL
});
```

**Избегайте в GraphQL:**
- ❌ Фильтры по дате (`where: { createdAt: { gte: ... } }`)
- ❌ Большие запросы с `payload` при фильтрах

## Реализация в DashboardComponent

```typescript
private loadChartData(): void {
  if (this.chartDateRange.fromDate && this.chartDateRange.toDate) {
    // Есть даты - используем REST
    this.restService.getAllMetrics(chartFilter, 100)
      .subscribe(...);
  } else {
    // Нет дат - используем GraphQL
    this.graphQLService.getMetrics(20, undefined, latestFilter)
      .subscribe(...);
  }
}
```

## Преимущества

1. **REST для дат:**
   - ✅ Нет проблем со сложностью GraphQL
   - ✅ Предсказуемая производительность
   - ✅ Легко кэшировать и оптимизировать

2. **GraphQL для остального:**
   - ✅ Гибкие запросы без дат
   - ✅ Агрегации и статистика
   - ✅ Реал-тайм обновления


