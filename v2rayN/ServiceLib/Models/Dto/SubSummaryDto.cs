namespace ServiceLib.Models.Dto;

/// <summary>订阅分组的汇总（左栏显示「节点数 · 平均延迟」用）</summary>
public class SubSummaryDto
{
    public string? SubId { get; set; }
    public int Cnt { get; set; }
    public double AvgDelay { get; set; }
}
