import { Component, OnInit, inject, computed, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ChartModule } from 'primeng/chart';
import { SharedModule } from 'primeng/api';
import { DragDropModule, CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { FranquiasService } from '../../../core/services/franquias.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, TableModule, ChartModule, DragDropModule, SharedModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  private franquiasService = inject(FranquiasService);
  private route = inject(ActivatedRoute);
  
  franquias = this.franquiasService.franquias;
  dashboardData = computed(() => {
    return this.franquiasService.dashboardData().map(d => ({
      ...d,
      TagList: d.TagsString ? d.TagsString.split(',').map(t => t.trim()) : []
    }));
  });

  widgets = signal<{ id: string, size: string }[]>([]);
  isEditMode = signal<boolean>(false);
  selectedCategory = signal<number | null>(null);

  constructor() {
    effect(() => {
      sessionStorage.setItem('dashboard-layout', JSON.stringify(this.widgets()));
    });
  }

  filteredDashboardData = computed(() => {
    const cat = this.selectedCategory();
    if (cat === null) return this.dashboardData();
    return this.dashboardData().filter(d => d.CategoriaID === cat);
  });

  totalMencoes = computed(() => {
    return this.filteredDashboardData().reduce((acc, curr) => acc + curr.VolumeMencoes, 0);
  });

  topFranquia = computed(() => {
    const data = this.filteredDashboardData();
    return data.length > 0 ? data[0].Nome : '-';
  });

  dynamicChartHeight = computed(() => {
    const count = this.filteredDashboardData().length;
    return count > 5 ? `${count * 45 + 100}px` : '400px';
  });

  chartData = computed(() => {
    const data = this.filteredDashboardData();
    return {
      labels: data.map(d => d.Nome),
      datasets: [
        {
          label: 'Hype Score',
          backgroundColor: '#3b82f6',
          borderRadius: 4,
          data: data.map(d => d.HypeScore),
          barPercentage: 0.7,
          categoryPercentage: 0.8
        }
      ]
    };
  });

  chartOptions: any = {
    indexAxis: 'y',
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: {
      x: { 
        ticks: { color: 'var(--text-color)' }, 
        grid: { color: 'var(--border-color)', drawBorder: false } 
      },
      y: { 
        ticks: { 
          color: 'var(--text-color)', 
          autoSkip: false,
          callback: function(this: any, value: any, index: number, values: any) {
            const label = this.getLabelForValue(value) as string;
            return label.length > 30 ? label.substring(0, 30) + '...' : label;
          }
        }, 
        grid: { display: false, drawBorder: false } 
      }
    }
  };

  pieChartData = computed(() => {
    const data = this.filteredDashboardData();
    const tagCount = new Map<string, number>();
    
    data.forEach(d => {
      d.TagList?.forEach(t => {
        if (t) tagCount.set(t, (tagCount.get(t) || 0) + 1);
      });
    });

    const sortedTags = Array.from(tagCount.entries()).sort((a, b) => b[1] - a[1]).slice(0, 5);

    return {
      labels: sortedTags.map(t => t[0]),
      datasets: [
        {
          data: sortedTags.map(t => t[1]),
          backgroundColor: ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6'],
          hoverBackgroundColor: ['#2563eb', '#059669', '#d97706', '#dc2626', '#7c3aed'],
          borderWidth: 0
        }
      ]
    };
  });

  pieChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { position: 'right', labels: { color: 'var(--text-color)', usePointStyle: true } }
    }
  };

  ngOnInit() {
    this.franquiasService.loadFranquias();
    this.franquiasService.loadDashboardData();

    this.route.queryParams.subscribe(params => {
      if (params['category']) {
        this.selectedCategory.set(Number(params['category']));
      } else {
        this.selectedCategory.set(null);
      }
    });

    const savedLayout = sessionStorage.getItem('dashboard-layout');
    if (savedLayout) {
      this.widgets.set(JSON.parse(savedLayout));
    } else {
      this.widgets.set([
        { id: 'info-metricas', size: 'widget-small' },
        { id: 'chart-tribos', size: 'widget-small' },
        { id: 'chart-hype', size: 'widget-large' },
        { id: 'table-franquias', size: 'widget-large' }
      ]);
    }
  }

  toggleEditMode() {
    this.isEditMode.update(v => !v);
  }

  drop(event: CdkDragDrop<{ id: string, size: string }[]>) {
    if (!this.isEditMode()) return;
    const currentWidgets = [...this.widgets()];
    moveItemInArray(currentWidgets, event.previousIndex, event.currentIndex);
    this.widgets.set(currentWidgets);
  }
}
