# Benchmark scenarios

`AccessorBenchmark` を 1 クラスに統合し、**「型 × プロパティ型」×「操作」×「処理種別」** の組み合わせを比較する。

## 比較軸

### 1. シナリオ（型 × プロパティ型）

| シナリオ | 宣言型 | プロパティ型 | 目的 |
|---|---|---|---|
| `ClassInt`    | class `Data`            | `int` (値型)    | 基本ケース |
| `ClassString` | class `Data`            | `string` (参照型) | ボックス化有無の差 |
| `Struct`      | struct `StructData`     | `int`           | 値型。object 経由(boxed)のみ |
| `Generic`     | class `GenericData<int>`| `int`           | ジェネリック型 |
| `Large`       | class `LargeData` (20プロパティ) | `int`  | メンバ数によるディスパッチコスト |

### 2. 操作

- `Get` … プロパティ読み取り
- `Set` … プロパティ書き込み

### 3. 処理種別（processing kind）

記述順は「速いと予測される順」に揃える: **Direct → AccessorCached → Accessor → Expression → Factory → ReflectionCached → Reflection**。

| 種別 | 内容 | キャッシュ |
|---|---|---|
| `Direct`           | 直接アクセス（`o.Id`）。各グループの **Baseline** | — |
| `AccessorCached`   | `IAccessor` インスタンスをキャッシュ | 有 |
| `Accessor`         | `IAccessor`(名前ベース・object) + `FindAccessor` を毎回 | 無 |
| `Expression`       | 実行時コンパイルした式木 delegate (`Func`/`Action`) | 有 |
| `Factory`          | 生成された typed delegate (`Func`/`Action`、`CreateGetter`/`CreateSetter`) | 有 |
| `ReflectionCached` | `PropertyInfo` をキャッシュ | 有 |
| `Reflection`       | `Type.GetProperty` + `GetValue/SetValue` を毎回 | 無 |

- `Factory` と `Expression` は同じ「typed delegate」だが、**コンパイル時生成 vs 実行時生成** の比較。
- `Accessor*` は名前ベースの動的 API（リフレクションの `GetValue/SetValue` に相当する利便性）。

## 適用可否（value type の制約）

- **Struct/Set** の `Factory` と `Expression` は対象外。
  - typed setter は値型をミューテートできない（delegate がコピーを受け取る）ため `CreateSetter<T>` は `null` を返す。
  - 式木の `Action<StructData,int>` も boxed インスタンスへ反映されない。
  - → Struct の書き込みは `Direct` / `Reflection(±)` / `Accessor(±)`（boxed 経由）のみ。
- Struct/Get の `Factory`/`Expression` は `Func<StructData,int>`（値渡し）で計測。

## 出力

- `[GroupBenchmarksBy(ByCategory)]` でシナリオ×操作ごとにグループ化。
- 各グループの `Direct` を `Baseline=true` とし、`Ratio` 列で相対性能を比較。
- `MemoryDiagnoser` でアロケーション（特に object API のボックス化）を確認。
