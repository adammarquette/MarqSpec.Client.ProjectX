# ADR-0012 — Order side stays on the wire numbers, and absence is null

**Status:** Accepted

## Context

`OrderSide` is `{ Bid = 0, Ask = 1 }` and was bound to a **non-nullable** `Side` on
`Order`, `HalfTrade`, `OrderUpdate` and `TradeNotification`. Zero is a real buy. A
payload that omitted `side` therefore deserialised as `Bid`, byte-identical to an
explicit `"side": 0`. There is no raw body on the returned models, so the
distinction was destroyed inside the client.

`MarqSpec.Mcp.TopstepX` maps `OrderSide` onto `VenueSide { Unknown, Buy, Sell }`.
For an absent field that `Unknown` arm is unreachable, so `get_orders` and
`get_trades` reported a confident Buy for an order the venue never gave a
direction to (gh#83). That is the same defect class as
[ADR-0011](0011-trade-direction-is-nullable.md): a missing fact wearing a
confident answer.

The other three order-shaped enums already have an unset zero (`OrderStatus.None`,
`OrderType.Unknown`, `PositionType.Undefined`). Adding one to `OrderSide` would
renumber `Bid`/`Ask` away from their swagger values (`0` / `1` on
`#/definitions/OrderSide`). `PlaceOrderRequest.side` is a required outbound
field on the same enum and must keep those numbers.

`R-10.5` requires a major bump **and** an ADR for a breaking public-surface
change. Making `Side` nullable is a source break; remapping the integers is a
silent semantic break. The source break is the one a compiler will catch.

## Decision

**Keep the documented wire numbers. Represent absence as null.**

1. `OrderSide` stays `Bid = 0`, `Ask = 1`.
2. `Side` is `OrderSide?` on the four **response** models: `Order`, `HalfTrade`,
   `OrderUpdate`, `TradeNotification`.
3. `PlaceOrderRequest.Side` stays a non-nullable `OrderSide`. It is an outbound
   required field.
4. An omitted `side` deserialises as `null`. An explicit JSON `null` still throws
   `JsonException`: swagger types the field as `OrderSide`, not
   nullable-and-present. Out-of-range integers (e.g. `9`) remain the enum cast.
5. Enums continue to travel as integers. Do not add a string-enum converter
   ([ADR-0001](0001-refit-typed-rest-client.md)).
6. The next published tag that includes this change is a **major**, because
   `Side` changing from `OrderSide` to `OrderSide?` is a source break. No file
   declares a version ([ADR-0006](0006-tag-driven-versioning.md)).

## Alternatives considered

**Renumber to `None`/`Unknown` = 0, `Bid` = 1, `Ask` = 2.** Rejected. It would
match `OrderType` / `PositionType` and still break every consumer that persisted
`(int)Side` or sent `0` meaning Bid. The swagger values would then be wrong.

**Keep `Side` non-nullable and add a second property (`HasSide`, raw int).**
Rejected. Zero remains the default, so omitted `side` is still Bid unless every
call site also checks the companion. Nullable is one place to look.

**Null `PlaceOrderRequest.Side` as well.** Rejected. Placement is an outbound
required field. An omitted side on the way *out* is a caller bug; defaulting it
to Bid is the existing (and remaining) behaviour.

**Map JSON `null` and out-of-range values to C# `null`, as ADR-0011 does for
`TradeLogType`.** Rejected for this field. swagger marks `side` required and not
nullable on `OrderModel` / `HalfTradeModel`; `"side": null` already threw, and
an out-of-range integer already bound as the cast. Changing either would be a
second behaviour change on top of the one this record exists to make.

## Consequences

- Consumers that wrote `OrderSide s = order.Side` stop compiling until they
  handle `null`.
- Consumers that wrote `order.Side == OrderSide.Bid` keep working for a stated
  Bid; an omitted `side` no longer matches.
- `MarqSpec.Mcp.TopstepX` can map `null` onto `VenueSide.Unknown` instead of
  reporting Buy.
- Hub events that carry an explicit `"side": null` still fail to bind (same as
  today). That is the swagger reading, not a new refusal.

## Decision log

*(none yet)*

## Follow-ups

None. The matching pin in `MarqSpec.Mcp.TopstepX` (gh#84 there) goes red when
this ships; that is their follow-up, not ours.
