import { Component, OnInit, ElementRef, ViewChild, inject, signal, effect, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AnalyticsService, CategorySales, ProductTrend } from '../../core/services/analytics.service';
import { forkJoin } from 'rxjs';

declare const d3: any;

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styles: [`
    :host { display: block; }
    .chart-container { position: relative; width: 100%; height: 320px; }
    
    ::ng-deep .d3-tooltip {
      position: absolute;
      text-align: center;
      padding: 8px 12px;
      font: 12px sans-serif;
      background: rgba(15, 23, 42, 0.9);
      color: #fff;
      border-radius: 6px;
      pointer-events: none;
      opacity: 0;
      transition: opacity 0.2s;
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
      z-index: 10;
    }
  `]
})
export class DashboardComponent implements OnInit {
  private analyticsService = inject(AnalyticsService);
  
  // Data Signals
  categorySales = signal<CategorySales[]>([]);
  trendingProducts = signal<ProductTrend[]>([]);
  isLoading = signal<boolean>(true);
  
  // KPI Computations
  totalRevenue = computed(() => this.categorySales().reduce((acc, curr) => acc + curr.TotalRevenue, 0));
  totalInvoices = computed(() => this.categorySales().reduce((acc, curr) => acc + curr.InvoiceCount, 0));
  topCategory = computed(() => {
    const sales = this.categorySales();
    return sales.length > 0 ? sales.sort((a, b) => b.TotalRevenue - a.TotalRevenue)[0].Category : 'N/A';
  });

  @ViewChild('chartContainer') chartContainer!: ElementRef;
  @ViewChild('barChartContainer') barChartContainer!: ElementRef;

  constructor() {
    effect(() => {
      const data = this.categorySales();
      if (data.length > 0 && !this.isLoading()) {
        setTimeout(() => {
          this.renderDonutChart(data);
          this.renderBarChart(data);
        }, 0);
      }
    });
  }

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.isLoading.set(true);
    
    // Set date range to last 12 months for a comprehensive view
    const endDate = new Date();
    const startDate = new Date();
    startDate.setFullYear(endDate.getFullYear() - 1); 

    // Fetch real data from backend
    forkJoin({
      categories: this.analyticsService.getCategorySales(startDate, endDate),
      trending: this.analyticsService.getTrendingProducts(startDate, endDate, 5)
    }).subscribe({
      next: (result) => {
        // If API returns empty lists, handle gracefully or keep empty defaults
        this.categorySales.set(result.categories || []);
        this.trendingProducts.set(result.trending || []);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Failed to load dashboard data from backend:', error);
        this.isLoading.set(false);
        // Fallback to empty state is handled by the template
      }
    });
  }

  renderDonutChart(data: CategorySales[]) {
    if (!this.chartContainer) return;
    const element = this.chartContainer.nativeElement;
    d3.select(element).selectAll("*").remove();

    const width = element.offsetWidth || 300;
    const height = 320;
    const margin = 20;
    const radius = Math.min(width, height) / 2 - margin;

    const svg = d3.select(element)
      .append("svg")
      .attr("width", width)
      .attr("height", height)
      .append("g")
      .attr("transform", `translate(${width / 2},${height / 2})`);

    const color = d3.scaleOrdinal()
      .domain(data.map(d => d.Category))
      .range(["#4f46e5", "#0ea5e9", "#8b5cf6", "#10b981", "#f59e0b"]);

    const pie = d3.pie()
      .value((d: any) => d.TotalRevenue)
      .sort(null)
      .padAngle(0.03);

    const arc = d3.arc()
      .innerRadius(radius * 0.65)
      .outerRadius(radius)
      .cornerRadius(4);

    const hoverArc = d3.arc()
      .innerRadius(radius * 0.65)
      .outerRadius(radius + 5)
      .cornerRadius(4);

    const tooltip = d3.select(element).append("div")
      .attr("class", "d3-tooltip");

    svg.selectAll("path")
      .data(pie(data))
      .enter()
      .append("path")
      .attr("d", arc)
      .attr("fill", (d: any) => color(d.data.Category))
      .style("cursor", "pointer")
      .style("transition", "opacity 0.2s")
      .on("mouseover", function(this: any, event: any, d: any) {
        d3.select(this).transition().duration(200).attr("d", hoverArc);
        tooltip.style("opacity", 1)
               .html(`<strong>${d.data.Category}</strong><br>$${d.data.TotalRevenue.toLocaleString()}`);
        
        const total = d3.sum(data, (x:any) => x.TotalRevenue);
        const percent = total > 0 ? Math.round((d.data.TotalRevenue / total) * 100) : 0;

        svg.append("text")
           .attr("class", "center-txt-val")
           .attr("text-anchor", "middle")
           .attr("dy", "0.1em")
           .style("font-size", "20px")
           .style("font-weight", "600")
           .style("fill", "#334155") 
           .text(`${percent}%`);
      })
      .on("mousemove", function(event: any) {
         // Adjust tooltip position relative to container
         const rect = element.getBoundingClientRect();
         tooltip.style("left", (event.clientX - rect.left + 10) + "px")
                .style("top", (event.clientY - rect.top - 20) + "px");
      })
      .on("mouseout", function(this: any) {
        d3.select(this).transition().duration(200).attr("d", arc);
        tooltip.style("opacity", 0);
        svg.selectAll(".center-txt-val").remove();
      });
  }

  renderBarChart(data: CategorySales[]) {
    if (!this.barChartContainer) return;
    const element = this.barChartContainer.nativeElement;
    d3.select(element).selectAll("*").remove();

    const margin = { top: 20, right: 20, bottom: 30, left: 50 };
    const width = (element.offsetWidth || 300) - margin.left - margin.right;
    const height = 320 - margin.top - margin.bottom;

    const svg = d3.select(element)
      .append("svg")
      .attr("width", width + margin.left + margin.right)
      .attr("height", height + margin.top + margin.bottom)
      .append("g")
      .attr("transform", `translate(${margin.left},${margin.top})`);

    const x = d3.scaleBand()
      .range([0, width])
      .padding(0.4)
      .domain(data.map(d => d.Category));

    const yMax = d3.max(data, (d: any) => d.InvoiceCount) || 10;
    const y = d3.scaleLinear()
      .range([height, 0])
      .domain([0, yMax * 1.1]);

    svg.append("g")
      .attr("class", "grid")
      .call(d3.axisLeft(y).tickSize(-width).tickFormat("")).style("stroke-dasharray", "3,3").style("stroke-opacity", 0.1);

    const defs = svg.append("defs");
    const gradient = defs.append("linearGradient").attr("id", "barGradient").attr("x1", "0%").attr("y1", "0%").attr("x2", "0%").attr("y2", "100%");
    gradient.append("stop").attr("offset", "0%").attr("stop-color", "#6366f1");
    gradient.append("stop").attr("offset", "100%").attr("stop-color", "#818cf8");

    svg.selectAll(".bar")
      .data(data)
      .enter().append("rect")
      .attr("class", "bar")
      .attr("x", (d: any) => x(d.Category))
      .attr("width", x.bandwidth())
      .attr("y", (d: any) => y(d.InvoiceCount))
      .attr("height", (d: any) => height - y(d.InvoiceCount))
      .style("fill", "url(#barGradient)")
      .attr("rx", 3);

    svg.append("g").attr("transform", `translate(0,${height})`).call(d3.axisBottom(x)).selectAll("text").style("text-anchor", "middle").style("font-size", "11px").style("fill", "#64748b");
    svg.append("g").call(d3.axisLeft(y).ticks(5)).selectAll("text").style("font-size", "11px").style("fill", "#64748b");
    svg.selectAll(".domain").remove();
  }
}