// Snake game for desktop (arrows) and mobile (swipe)
(function() {
    const canvas = document.getElementById('snake-canvas');
    const ctx = canvas.getContext('2d');
    
    const GAME_SIZE = 600; 
    
    let tile = 20; 
    let cols = Math.floor(GAME_SIZE / tile);
    let rows = Math.floor(GAME_SIZE / tile);
    
    let snake, direction, nextDirection, foods, gameOver, moveInterval = 97, lastMove, score;
    let highScores = null;
    let scoreSubmitted = false;
    
    let foodSpawnInterval = 3000;
    let foodSpawnTimer = null;

    // Powerup state
    let isInvincible = false;
    let invincibleTimer = null;
    let invincibleMoveInterval = Math.max(40, Math.floor(moveInterval * 0.5));
    let invincibleDuration = 5000;
    let invincibleColorIdx = 0;
    let invincibleColors = [
        '#ff0000', '#ffa500', '#ffff00', '#00ff00', '#00ffff', '#0000ff', '#8a2be2', '#ff69b4'
    ];
    let lastBlueFoodScore = 0;
    
    let normalMoveInterval = moveInterval;
    let normalFoodSpawnInterval = 3000;
    let fastFoodSpawnInterval = 1000;

    function resize() {
        // Set fixed size, do not use window size
        canvas.width = GAME_SIZE;
        canvas.height = GAME_SIZE;
    }

    function reset() {
        direction = 'right';
        nextDirection = 'right';
        snake = [
            {x: 5, y: 5},
            {x: 4, y: 5},
            {x: 3, y: 5}
        ];
        foods = [];
        spawnFood();
        if (foodSpawnTimer) clearInterval(foodSpawnTimer);
        foodSpawnInterval = normalFoodSpawnInterval;
        foodSpawnTimer = setInterval(spawnFood, foodSpawnInterval);
        gameOver = false;
        score = 0;
        lastBlueFoodScore = 0;
        lastMove = Date.now();
        highScores = null;
        scoreSubmitted = false;
        // Reset power-up state
        isInvincible = false;
        if (invincibleTimer) clearTimeout(invincibleTimer);
        moveInterval = normalMoveInterval;
    }

    function spawnFood(forceBlue) {
        // If not invincible, only spawn if less than 5 foods
        if (!isInvincible && foods.length >= 5) return;
        let attempts = 0;
        while (attempts < 100) {
            let isBlue = false;
            if (forceBlue) {
                isBlue = true;
            } else {
                // Only spawn blue food randomly if not forced
                isBlue = Math.random() < 0.1 && !foods.some(f => f.type === 'blue');
            }
            let f = {
                x: Math.floor(Math.random() * cols),
                y: Math.floor(Math.random() * rows),
                color: isBlue ? '#2196f3' : '#e74c3c',
                type: isBlue ? 'blue' : 'normal'
            };
            if (!snake.some(s => s.x === f.x && s.y === f.y) && !foods.some(food => food.x === f.x && food.y === f.y)) {
                foods.push(f);
                break;
            }
            attempts++;
        }
    }

    function showHighScores(scores) {
        ctx.save();
        ctx.globalAlpha = 0.95;
        ctx.fillStyle = '#222';
        ctx.fillRect(canvas.width/2-200, canvas.height/2-120, 400, 340);
        ctx.globalAlpha = 1;
        ctx.fillStyle = '#fff';
        ctx.font = 'bold 32px Arial';
        ctx.textAlign = 'center';
        ctx.fillText('Top 5 High Scores', canvas.width/2, canvas.height/2-70);
        ctx.font = '24px Arial';
        for (let i = 0; i < scores.length; i++) {
            const s = scores[i];
            let name = s.playerName || 'Anonymous';
            ctx.fillText(`${i+1}. ${name}: ${s.score}`, canvas.width/2, canvas.height/2-30 + i*32);
        }
        // Show last score above the restart message
        ctx.font = 'bold 18px Arial';
        ctx.fillStyle = '#ffd700';
        ctx.fillText(`Your score: ${score}`, canvas.width/2, canvas.height/2+110);
        ctx.fillStyle = '#fff';
        ctx.font = '18px Arial';
        ctx.fillText('Tap or press any key to restart', canvas.width/2, canvas.height/2+140);
        ctx.textAlign = 'left';
        ctx.restore();

        // Draw the home button below the restart message
        drawHomeButton(canvas.width/2, canvas.height/2+160);
    }

    function drawHomeButton(x, y) {
        // Draw a button-like rectangle
        const btnWidth = 180, btnHeight = 40;
        ctx.save();
        ctx.beginPath();
        ctx.roundRect(x - btnWidth/2, y, btnWidth, btnHeight, 8);
        ctx.fillStyle = '#fff';
        ctx.globalAlpha = 0.9;
        ctx.fill();
        ctx.globalAlpha = 1;
        ctx.lineWidth = 2;
        ctx.strokeStyle = '#198754'; // Bootstrap success
        ctx.stroke();
        ctx.font = 'bold 20px Arial';
        ctx.fillStyle = '#198754';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText('Back to Home', x, y + btnHeight/2);
        ctx.restore();
    }

    function submitScore() {
        if (scoreSubmitted) return;
        scoreSubmitted = true;
        fetch('/Home/CreateSnakeScore', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ score: score })
        })
        .then(r => r.json())
        .then(scores => {
            highScores = scores;
            draw();
        })
        .catch(() => {
            highScores = [];
            draw();
        });
    }

    function draw() {
        ctx.fillStyle = '#111';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        // Draw all foods
        for (let f of foods) {
            ctx.fillStyle = f.color;
            ctx.fillRect(f.x * tile, f.y * tile, tile, tile);
        }
        // Draw snake
        if (isInvincible) {
            for (let i = 0; i < snake.length; i++) {
                ctx.fillStyle = invincibleColors[(invincibleColorIdx + i) % invincibleColors.length];
                ctx.fillRect(snake[i].x * tile, snake[i].y * tile, tile, tile);
            }
        } else {
            ctx.fillStyle = '#2ecc40';
            for (let s of snake) {
                ctx.fillRect(s.x * tile, s.y * tile, tile, tile);
            }
        }
        if (gameOver) {
            if (highScores) {
                showHighScores(highScores);
            } else {
                ctx.fillStyle = '#fff';
                ctx.font = 'bold 48px Arial';
                ctx.textAlign = 'center';
                ctx.fillText('Game Over', canvas.width/2, canvas.height/2);
                ctx.font = 'bold 24px Arial';
                ctx.fillText('Tap or press any key to restart', canvas.width/2, canvas.height/2 + 40);
                ctx.textAlign = 'left';
                submitScore();
            }
        }
        // Update score in the external div
        const scoreDiv = document.getElementById('snake-score-label');
        if (scoreDiv) {
            scoreDiv.textContent = 'Score: ' + score;
        }
    }

    function getNextHead(dir, head) {
        let h = {...head};
        if (dir === 'left') h.x--;
        if (dir === 'right') h.x++;
        if (dir === 'up') h.y--;
        if (dir === 'down') h.y++;
        return h;
    }

    function isOutOfBounds(h) {
        return h.x < 0 || h.x >= cols || h.y < 0 || h.y >= rows;
    }

    function isOpposite(dir1, dir2) {
        return (dir1 === 'left' && dir2 === 'right') ||
               (dir1 === 'right' && dir2 === 'left') ||
               (dir1 === 'up' && dir2 === 'down') ||
               (dir1 === 'down' && dir2 === 'up');
    }

    function getValidDirection(head, currentDir) {
        // Try directions in order: right, down, left, up (clockwise)
        const dirs = ['right', 'down', 'left', 'up'];
        for (let dir of dirs) {
            if (dir === currentDir || isOpposite(dir, currentDir)) continue;
            let nh = getNextHead(dir, head);
            if (!isOutOfBounds(nh)) return dir;
        }
        // If no other, try currentDir
        let nh = getNextHead(currentDir, head);
        if (!isOutOfBounds(nh)) return currentDir;
        // Fallback: stay in place (should never happen)
        return currentDir;
    }

    function activateInvincible() {
        isInvincible = true;
        moveInterval = invincibleMoveInterval;
        if (invincibleTimer) clearTimeout(invincibleTimer);
        invincibleTimer = setTimeout(deactivateInvincible, invincibleDuration);
        // Make food spawn faster
        if (foodSpawnTimer) clearInterval(foodSpawnTimer);
        foodSpawnInterval = fastFoodSpawnInterval;
        foodSpawnTimer = setInterval(spawnFood, foodSpawnInterval);
    }
    function deactivateInvincible() {
        isInvincible = false;
        moveInterval = normalMoveInterval;
        // Restore normal food spawn interval
        if (foodSpawnTimer) clearInterval(foodSpawnTimer);
        foodSpawnInterval = normalFoodSpawnInterval;
        foodSpawnTimer = setInterval(spawnFood, foodSpawnInterval);
    }

    function move() {
        if (gameOver) return;
        let head = {...snake[0]};
        let dir = nextDirection;
        let nextHead = getNextHead(dir, head);
        if (isInvincible && isOutOfBounds(nextHead)) {
            // Redirect along the wall
            dir = getValidDirection(head, dir);
            nextDirection = dir;
            nextHead = getNextHead(dir, head);
        }
        direction = dir;
        head = nextHead;
        // Wall collision
        if (!isInvincible && isOutOfBounds(head)) {
            gameOver = true;
            if (foodSpawnTimer) clearInterval(foodSpawnTimer);
            draw();
            return;
        }
        // Self collision
        if (!isInvincible && snake.some(s => s.x === head.x && s.y === head.y)) {
            gameOver = true;
            if (foodSpawnTimer) clearInterval(foodSpawnTimer);
            draw();
            return;
        }
        snake.unshift(head);
        // Check if head is on any food
        let foodIdx = foods.findIndex(f => f.x === head.x && f.y === head.y);
        if (foodIdx !== -1) {
            let food = foods[foodIdx];
            foods.splice(foodIdx, 1);
            handleFoodScoreLogic(food);
        } else {
            // Remove the tail if no food eaten (normal snake movement)
            snake.pop();
        }
    }

    function handleFoodScoreLogic(food) {
        score++;
        // Blue food logic
        if (food.type === 'blue') {
            activateInvincible();
        }
        // Blue food every 100 points
        if (score > 0 && score % 100 === 0 && lastBlueFoodScore !== score && !foods.some(f => f.type === 'blue')) {
            spawnFood(true); // force blue food
            lastBlueFoodScore = score;
        }
        if (isInvincible) {
            invincibleColorIdx = (invincibleColorIdx + 1) % invincibleColors.length;
        }
    }

    function keyDownHandler(e) {
        if (gameOver) {
            // Only restart if key is not an arrow key or WASD or F5
            const forbidden = ['ArrowUp','ArrowDown','ArrowLeft','ArrowRight','w','a','s','d','W','A','S','D','F5'];
            if (!forbidden.includes(e.key)) {
                reset();
                draw();
            }
            return;
        }
        // Arrow keys and WASD
        if (e.key === 'ArrowUp' || e.key === 'w') {
            if (direction !== 'down') nextDirection = 'up';
        } else if (e.key === 'ArrowDown' || e.key === 's') {
            if (direction !== 'up') nextDirection = 'down';
        } else if (e.key === 'ArrowLeft' || e.key === 'a') {
            if (direction !== 'right') nextDirection = 'left';
        } else if (e.key === 'ArrowRight' || e.key === 'd') {
            if (direction !== 'left') nextDirection = 'right';
        }
    }

    function touchStartHandler(e) {
        if (gameOver) {
            reset();
            draw();
            return;
        }
        // Get touch position
        let touch = e.touches[0];
        let rect = canvas.getBoundingClientRect();
        let x = Math.floor((touch.clientX - rect.left) / tile);
        let y = Math.floor((touch.clientY - rect.top) / tile);
        // Find direction to move
        if (x < snake[0].x) {
            // Move left
            if (direction !== 'right') nextDirection = 'left';
        } else if (x > snake[0].x) {
            // Move right
            if (direction !== 'left') nextDirection = 'right';
        } else if (y < snake[0].y) {
            // Move up
            if (direction !== 'down') nextDirection = 'up';
        } else if (y > snake[0].y) {
            // Move down
            if (direction !== 'up') nextDirection = 'down';
        }
    }

    function touchMoveHandler(e) {
        e.preventDefault();
    }

    function touchEndHandler(e) {
        e.preventDefault();
    }

    window.addEventListener('keydown', keyDownHandler);
    canvas.addEventListener('touchstart', touchStartHandler);
    canvas.addEventListener('touchmove', touchMoveHandler);
    canvas.addEventListener('touchend', touchEndHandler);

    let lastMoveTime = 0;
    function gameLoop(ts) {
        if (!lastMoveTime) lastMoveTime = ts;
        if (!gameOver && ts - lastMoveTime > moveInterval) {
            move();
            draw();
            lastMoveTime = ts;
        }
        requestAnimationFrame(gameLoop);
    }

    resize();
    reset();
    draw();
    requestAnimationFrame(gameLoop);
})();
