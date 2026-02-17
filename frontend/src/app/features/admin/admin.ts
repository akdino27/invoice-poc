import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { JobService } from '../../core/services/job.service';
import { AdminService } from '../../core/services/admin.service';
import { LogsService } from '../../core/services/logs.service';
import { Job } from '../../shared/models/job.model';
import { User } from '../../shared/models/user.model';
import { FileChangeLog } from '../../shared/models/log.model';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin.html',
  styleUrls: ['./admin.css'],
})
export class Admin implements OnInit {
  activeTab = signal<'jobs' | 'users' | 'logs'>('users');
  
  // Users
  pendingUsers = signal<User[]>([]);
  allUsers = signal<User[]>([]);
  usersLoading = signal(false);
  
  // Jobs
  jobs = signal<Job[]>([]);
  jobsLoading = signal(false);
  
  // Logs
  logs = signal<FileChangeLog[]>([]);
  logsLoading = signal(false);
  logPage = signal(1);
  logTotal = signal(0);

  constructor(
    private adminService: AdminService,
    private jobService: JobService,
    private logsService: LogsService
  ) {}

  ngOnInit() {
    this.loadUsers();
  }

  switchTab(tab: 'jobs' | 'users' | 'logs') {
    this.activeTab.set(tab);
    if (tab === 'users') this.loadUsers();
    if (tab === 'jobs') this.loadJobs();
    if (tab === 'logs') this.loadLogs();
  }

  // --- Users ---
  loadUsers() {
    this.usersLoading.set(true);
    // Load Pending
    this.adminService.getPendingUsers().subscribe({
      next: (data) => this.pendingUsers.set(data),
      error: (e) => console.error(e)
    });
    // Load All
    this.adminService.getAllUsers().subscribe({
      next: (data) => {
        this.allUsers.set(data);
        this.usersLoading.set(false);
      },
      error: (e) => {
        console.error(e);
        this.usersLoading.set(false);
      }
    });
  }

  approveUser(id: string) {
    if(!confirm('Approve user?')) return;
    this.adminService.approveUser(id).subscribe(() => this.loadUsers());
  }

  rejectUser(id: string) {
    const reason = prompt('Rejection reason:');
    if (!reason) return;
    this.adminService.rejectUser(id, reason).subscribe(() => this.loadUsers());
  }

  unlockUser(id: string) {
    this.adminService.unlockUser(id).subscribe(() => {
      alert('User unlocked');
      this.loadUsers();
    });
  }
  
  deleteUser(id: string) {
    if(!confirm('Delete user?')) return;
    this.adminService.deleteUser(id).subscribe(() => this.loadUsers());
  }

  // --- Jobs ---
  loadJobs() {
    this.jobsLoading.set(true);
    this.jobService.getJobs(undefined, 1, 50).subscribe({
      next: (res) => {
        this.jobs.set(res.jobs);
        this.jobsLoading.set(false);
      }
    });
  }

  requeueJob(id: string) {
    this.jobService.requeueJob(id).subscribe(() => {
      alert('Job requeued');
      this.loadJobs();
    });
  }

  // --- Logs ---
  loadLogs() {
    this.logsLoading.set(true);
    this.logsService.getLogs(this.logPage(), 20).subscribe({
      next: (res) => {
        this.logs.set(res.logs);
        this.logTotal.set(res.total);
        this.logsLoading.set(false);
      }
    });
  }

  nextLogPage() {
    this.logPage.update(p => p + 1);
    this.loadLogs();
  }
  
  prevLogPage() {
    if(this.logPage() > 1) {
      this.logPage.update(p => p - 1);
      this.loadLogs();
    }
  }

  // Helpers
  formatDate(date: string | null) {
    if (!date) return '-';
    return new Date(date).toLocaleString();
  }
}