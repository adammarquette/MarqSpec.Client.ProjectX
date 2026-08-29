# ADR-0011 — Trade direction stays on the wire numbers, and absence is null

**Status:** Accepted

## Context

`TradeUpdate.Type` was a non-nullable `TradeLogType { Buy = 0, Sell = 1 }`. An omitted, null, or
unparseable `type` therefore deserialised as **Buy**. For order-flow work that is not cosmetic:
every unstated print becomes buying pressure, and the resulting delta reads as genuine (gh#86).

The preferred shape in the issue was `Unknown = 0`, `Buy = 1`, `Sell = 2`, matching `DomType` /
`OrderType` in this library. That numbering is only safe if the live wire already uses zero for
"not recognised".

The live wire does not. The ProjectX Real Time overview
([`gateway.docs.projectx.com/docs/realtime`](https://gateway.docs.projectx.com/docs/realtime/))
documents:

```js
{ symbolId: "F.US.EP", price: 2100.25, timestamp: "2024-07-21T13:45:00Z", type: 0, /* Buy */ volume: 2 }
```

```cs
public enum TradeLogType { Buy = 0, Sell = 1 }
```

`swagger.json` does not describe hub payloads — it is REST-only — and this repository holds no
captured tape that contradicts the published enum. A bare renumber would map a stated Buy onto
`Unknown`. A converter that remaps wire `0 → Buy=1` would keep the tape correct and still change
the public integer of `Buy`, which is a break for anyone who persisted `(int)Type`.

`R-10.5` requires a major bump **and** an ADR for a breaking public-surface change. Making `Type`
nullable is a source break; remapping the integers is a silent semantic break. The source break is
the one a compiler will catch.

## Decision

**Keep the documented wire numbers. Represent absence as null.**

1. `TradeLogType` stays `Buy = 0`, `Sell = 1`.
2. `TradeUpdate.Type` is `TradeLogType?`.
3. A converter maps wire `0` / `1` onto those members and maps omitted, null, unrecognised, or
   unparseable tokens to `null` — never to `Buy`, and never by throwing (a throw drops the whole
   trade at the hub bind).
4. The next published tag that includes this change is a **major**, because `Type` changing from
   `TradeLogType` to `TradeLogType?` is a source break. No file declares a version
   ([ADR-0006](0006-tag-driven-versioning.md)).

Hub `contractId` is surfaced as an additive `ContractId` property on `TradeUpdate`, `PriceUpdate`
and `OrderBookUpdate`, stamped at bind time. That part is not a break and does not need its own
record.

## Alternatives considered

**Renumber to `Unknown = 0`, `Buy = 1`, `Sell = 2` with a wire mapping.** Rejected. It matches
house style and still requires a converter because the venue sends `type: 0` meaning Buy. The
public integers would then disagree with the official ProjectX enum this library mirrors, and
`(int)TradeLogType.Buy` would silently change meaning.

**Bare renumber without a converter.** Rejected. That is the original bug with the labels swapped:
every live Buy arrives as Unknown.

**Keep `Type` non-nullable and add `Unknown = 2` (or a second property).** Rejected. Zero remains
the default, so omitted `type` is still Buy.

**Event-args types carrying `(contractId, update)` instead of stamping `ContractId`.** Rejected as
the larger break. `EventHandler<TradeUpdate>` becoming `EventHandler<TradeUpdateReceivedEventArgs>`
forces every consumer to recompile for no extra information.

## Consequences

- Consumers that wrote `TradeLogType t = update.Type` stop compiling until they handle `null`.
- Consumers that wrote `update.Type == TradeLogType.Buy` keep working for a stated Buy; an omitted
  `type` no longer matches.
- A consumer subscribed to two expiries of one root can attribute every quote, depth row and print
  via `ContractId`.
- Re-subscription after reconnect is **not** decided here. That is gh#87 and collides on the same
  hub types.

## Decision log

*(none yet)*

## Follow-ups

- gh#87 — the market hub reconnects but never re-subscribes, so the tape goes silent while
  reporting `Connected`. Out of scope for this change; do not take it in the same tree.
