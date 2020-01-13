using System;
using System.Linq;
using BotKit.Core;

namespace BotKitStrategies
{
    /// <summary>
    /// 元祖ドテン君（オープニングレンジ・ブレイクアウト戦略）
    /// 参考：https://twitter.com/blog_uki/status/981768546429448192
    /// </summary>
    [StrategyExport("元祖ドテン君")]
    public class OpeningRangeBreakOut : BaseStrategy
    {
        private TimeFrame TimeFrame { get; set; }
        public override void Init()
        {
            // 2時間足のタイムフレームを作成してストラテジの要求タイムフレームに追加
            TimeFrame = new TimeFrame(new TimeSpan(2, 0, 0));
            TimeFrames.Add(TimeFrame);

            OhlcBarFixed += OpeningRangeBreakOut_OhlcBarFixedAsync;
        }

        private async void OpeningRangeBreakOut_OhlcBarFixedAsync(object sender, OhlcBarFixedEventArgs e)
        {
            // ブレイクアウト判定係数
            var breakOutThreshold = 1.6;
            // レンジ算出期間
            var rangePeriod = 5;

            // 2時間足でない場合はスキップ
            if (!e.Ohlc.TimeFrame.Equals(TimeFrame)) return;
            // レンジ算出期間 + 1本の確定足がなければスキップ
            if (e.OhlcSeries.Count < rangePeriod + 1) return;

            // レンジ幅の算出
            var rangeWidth = e.OhlcSeries
                // 最後の(レンジ算出期間)本分の足を取得
                .Skip(e.OhlcSeries.Count - (rangePeriod + 1))
                .Take(rangePeriod)
                // 高値-安値を計算
                .Select(ohlc => ohlc.High - ohlc.Low)
                // 平均を取る
                .Average();

            if (e.Ohlc.IsBullish() && e.Ohlc.High - e.Ohlc.Open > rangeWidth * breakOutThreshold && Engine.CurrentPositionSize <= 0)
            {
                // 既存ポジションは決済
                await Engine.CloseAllOrdersAsync(e.Ohlc.Close, e.Ohlc.TimestampTo);
                // 陽線、かつ(高値-始値)が(レンジ幅*ブレイクアウト判定係数)より大きければ買い
                await Engine.PostOrderAsync(OrderSide.Buy, OrderType.Market, 1, e.Ohlc.Close, 0, e.Ohlc.TimestampTo);
            }
            else if (e.Ohlc.IsBearish() && e.Ohlc.Open - e.Ohlc.Low > rangeWidth * breakOutThreshold && Engine.CurrentPositionSize >= 0)
            {
                // 既存ポジションは決済
                await Engine.CloseAllOrdersAsync(e.Ohlc.Close, e.Ohlc.TimestampTo);
                // 陰線、かつ(高値-始値)が(レンジ幅*ブレイクアウト判定係数)より大きければ売り
                await Engine.PostOrderAsync(OrderSide.Sell, OrderType.Market, 1, e.Ohlc.Close, 0, e.Ohlc.TimestampTo);
            }
        }
    }
}
