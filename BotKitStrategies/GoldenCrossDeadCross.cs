using System;
using System.Linq;
using BotKit.Core;
using BotKit.Core.Indicators;

namespace BotKitStrategies
{
    [StrategyExport("GC,DC sample")]
    public class GoldenCrossDeadCrossStrategy : BaseStrategy
    {
        private SimpleMovingAverageIndicator ShortMovingAverageIndicator { get; set; }
        private SimpleMovingAverageIndicator LongMovingAverageIndicator { get; set; }
        private TimeFrame TimeFrame { get; set; }
        public override void Init()
        {
            // 1日足（24時間足）のタイムフレームを作成してストラテジの要求タイムフレームに追加
            TimeFrame = new TimeFrame(new TimeSpan(24, 0, 0));
            TimeFrames.Add(TimeFrame);

            // 5日単純移動平均線と21日単純移動平均線をインディケータ一覧に追加
            LongMovingAverageIndicator = new SimpleMovingAverageIndicator(TimeFrame, 21, OhlcValueProperty.Close);
            ShortMovingAverageIndicator = new SimpleMovingAverageIndicator(TimeFrame, 5, OhlcValueProperty.Close);
            Indicators.Add(LongMovingAverageIndicator);
            Indicators.Add(ShortMovingAverageIndicator);

            // 5日単純移動平均線が更新された際のイベントハンドラ
            ShortMovingAverageIndicator.IndicatorValueUpdated += ShortMovingAverage_IndicatorValueUpdatedAsync;
        }
        private async void ShortMovingAverage_IndicatorValueUpdatedAsync(object sender, IndicatorValueUpdatedEventArgs<float> e)
        {
            // 長期間の単純移動平均線が2期間経過していなければ売買しない
            if (LongMovingAverageIndicator.Values.Count < 2) return;

            // 一つ前の単純移動平均線の値
            var prevLongMA = LongMovingAverageIndicator.Values.Skip(LongMovingAverageIndicator.Values.Count - 2).First();
            var prevShortMA = e.Values.Skip(e.Values.Count - 2).First();
            // 新たに更新された現在の単純移動平均線の値
            var newLongMA = LongMovingAverageIndicator.Value;
            var newShortMA = e.Value;

            var ohlc = OhlcSerieses[TimeFrame].Last();
            if (prevShortMA < prevLongMA && newShortMA > newLongMA)
            {
                // 既存ポジションは決済
                await Engine.CloseAllOrdersAsync(ohlc.Close, ohlc.TimestampTo);
                // ゴールデンクロス
                await Engine?.PostOrderAsync(OrderSide.Buy, OrderType.Market, 1, ohlc.Close, 0, ohlc.TimestampTo);
            }
            else if (prevShortMA > prevLongMA && newShortMA < newLongMA)
            {
                // 既存ポジションは決済
                await Engine.CloseAllOrdersAsync(ohlc.Close, ohlc.TimestampTo);
                // デッドクロス
                await Engine?.PostOrderAsync(OrderSide.Sell, OrderType.Market, 1, ohlc.Close, 0, ohlc.TimestampTo);
            }
        }
    }
}
